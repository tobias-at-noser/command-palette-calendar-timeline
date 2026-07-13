using CalendarTimeline.Core;
using System.Runtime.InteropServices;

namespace CalendarTimeline.Worker;

public sealed class OutlookCalendarSnapshotSource : ICalendarSnapshotSource
{
    public Task<CalendarSnapshot> LoadSnapshotAsync(DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

#if WINDOWS
        var result = LoadAppointments(now, cancellationToken);
        return Task.FromResult(OutlookAppointmentMapper.CreateSnapshot(
            now,
            result.Appointments,
            result.HadFailures ? "Einige Kalender nicht verfügbar." : null));
#else
        throw new PlatformNotSupportedException("Outlook COM calendar loading requires Windows and Outlook Desktop.");
#endif
    }

#if WINDOWS
    private static CalendarLoadResult LoadAppointments(DateTimeOffset now, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var outlookType = Type.GetTypeFromProgID("Outlook.Application")
            ?? throw new InvalidOperationException("Outlook Desktop is not installed or not registered.");
        object? outlook = null;
        object? outlookNamespace = null;
        object? defaultStore = null;

        try
        {
            outlook = Activator.CreateInstance(outlookType)
                ?? throw new InvalidOperationException("Outlook Desktop could not be initialized.");
            outlookNamespace = ((dynamic)outlook).GetNamespace("MAPI");
            defaultStore = ((dynamic)outlookNamespace).DefaultStore;
            var defaultStoreId = Convert.ToString(((dynamic)defaultStore).StoreID);
            object? rootFolder = null;
            var rootFolderTransferred = false;
            var calendarFolders = new List<object>();

            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                rootFolder = ((dynamic)defaultStore).GetRootFolder();
                rootFolderTransferred = CollectCalendarFolders(rootFolder, defaultStoreId, calendarFolders, cancellationToken);

                var appointments = new List<OutlookAppointmentData>();
                var hadCalendarLoadFailures = false;
                var successfulFolderCount = 0;
                foreach (var calendarFolder in calendarFolders)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    try
                    {
                        appointments.AddRange(LoadCalendarAppointments(calendarFolder, outlookNamespace, now, cancellationToken));
                        successfulFolderCount++;
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                    catch
                    {
                        hadCalendarLoadFailures = true;
                    }
                }

                if (successfulFolderCount == 0)
                {
                    throw new InvalidOperationException("No personal calendar folders could be loaded.");
                }

                return new CalendarLoadResult(appointments, hadCalendarLoadFailures);
            }
            finally
            {
                foreach (var calendarFolder in calendarFolders)
                {
                    ReleaseComObject(calendarFolder);
                }

                if (!rootFolderTransferred && !IsRetainedCalendarFolder(rootFolder, calendarFolders))
                {
                    ReleaseComObject(rootFolder);
                }
            }
        }
        finally
        {
            ReleaseComObject(defaultStore);
            ReleaseComObject(outlookNamespace);
            ReleaseComObject(outlook);
        }
    }

    private static bool CollectCalendarFolders(
        dynamic folder,
        string? defaultStoreId,
        List<object> calendarFolders,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        object? folders = null;
        var isCalendar = false;

        try
        {
            isCalendar = Convert.ToInt32(folder.DefaultItemType) == 1 &&
                string.Equals(Convert.ToString(folder.StoreID), defaultStoreId, StringComparison.Ordinal);
            if (isCalendar)
            {
                calendarFolders.Add(folder);
            }

            folders = folder.Folders;
            var count = Convert.ToInt32(((dynamic)folders).Count);
            for (var index = 1; index <= count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                object? child = null;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    child = ((dynamic)folders).Item(index);
                    CollectCalendarFolders(child, defaultStoreId, calendarFolders, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // An unreadable child does not prevent discovery of later folders.
                }
                finally
                {
                    if (!IsRetainedCalendarFolder(child, calendarFolders))
                    {
                        ReleaseComObject(child);
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            // A folder that cannot be inspected was never discovered as a calendar and does not affect partial status.
        }
        finally
        {
            ReleaseComObject(folders);
        }

        return isCalendar;
    }

    private static bool IsRetainedCalendarFolder(object? folder, List<object> calendarFolders)
    {
        return folder is not null && calendarFolders.Any(retainedFolder => ReferenceEquals(retainedFolder, folder));
    }

    private static IReadOnlyList<OutlookAppointmentData> LoadCalendarAppointments(
        object calendarFolder,
        object outlookNamespace,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        dynamic folder = calendarFolder;
        var calendarId = Convert.ToString(folder.EntryID) ?? Convert.ToString(folder.FolderPath) ?? string.Empty;
        var calendarName = Convert.ToString(folder.Name) ?? string.Empty;
        var calendarColor = TryGetCalendarColor(calendarFolder);
        object? items = null;
        object? restrictedItems = null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            items = folder.Items;
            dynamic outlookItems = items;
            outlookItems.IncludeRecurrences = true;
            outlookItems.Sort("[Start]");

            var window = CalendarWindow.Create(now);
            var filter = $"[End] > '{window.Start.LocalDateTime:g}' AND [Start] < '{window.End.LocalDateTime:g}'";
            restrictedItems = outlookItems.Restrict(filter);
            var appointments = new List<OutlookAppointmentData>();
            var count = Convert.ToInt32(((dynamic)restrictedItems).Count);

            for (var index = 1; index <= count; index++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                object? item = null;
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    item = ((dynamic)restrictedItems).Item(index);
                    dynamic appointment = item;
                    DateTime start = appointment.Start;
                    DateTime end = appointment.End;
                    var sensitivity = Convert.ToInt32(appointment.Sensitivity);
                    appointments.Add(new OutlookAppointmentData(
                        Convert.ToString(appointment.EntryID) ?? Guid.NewGuid().ToString("N"),
                        Convert.ToString(appointment.Subject) ?? string.Empty,
                        Convert.ToString(appointment.Location) ?? string.Empty,
                        new DateTimeOffset(start),
                        new DateTimeOffset(end),
                        sensitivity == 2,
                        sensitivity == 3,
                        Convert.ToString(appointment.Body),
                        calendarId,
                        calendarName,
                        calendarColor,
                        LoadCategories(appointment, outlookNamespace)));
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch
                {
                    // A malformed or inaccessible item must not make its calendar unreadable.
                }
                finally
                {
                    ReleaseComObject(item);
                }
            }

            return appointments;
        }
        finally
        {
            ReleaseComObject(restrictedItems);
            ReleaseComObject(items);
        }
    }

    private static IReadOnlyList<CalendarCategory> LoadCategories(dynamic item, object outlookNamespace)
    {
        string? categoriesValue;
        try
        {
            categoriesValue = Convert.ToString(item.Categories);
        }
        catch
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(categoriesValue))
        {
            return [];
        }

        var categories = new List<CalendarCategory>();
        foreach (var categoryName in categoriesValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            categories.Add(new CalendarCategory(categoryName, TryGetCategoryColor(categoryName, outlookNamespace)));
        }

        return categories;
    }

    private static string? TryGetCalendarColor(object calendarFolder)
    {
        try
        {
            return OutlookColorToHex(Convert.ToInt32(((dynamic)calendarFolder).CalendarColor));
        }
        catch
        {
            return null;
        }
    }

    private static string? TryGetCategoryColor(string categoryName, object outlookNamespace)
    {
        object? categories = null;
        object? category = null;

        try
        {
            categories = ((dynamic)outlookNamespace).Categories;
            category = ((dynamic)categories).Item(categoryName);
            return OutlookColorToHex(Convert.ToInt32(((dynamic)category).Color));
        }
        catch
        {
            return null;
        }
        finally
        {
            ReleaseComObject(category);
            ReleaseComObject(categories);
        }
    }

    private static void ReleaseComObject(object? value)
    {
        try
        {
            if (value is not null && Marshal.IsComObject(value))
            {
                Marshal.FinalReleaseComObject(value);
            }
        }
        catch (COMException)
        {
            // Cleanup must not hide the original Outlook operation failure.
        }
        catch (InvalidComObjectException)
        {
            // The RCW may already have been released by Outlook during an earlier failure.
        }
    }

    private static string? OutlookColorToHex(int color) => color switch
    {
        1 => "#E81123", 2 => "#FF8C00", 3 => "#FFB900", 4 => "#FFF100", 5 => "#498205",
        6 => "#00B7C3", 7 => "#7FBA00", 8 => "#0078D4", 9 => "#5C2D91", 10 => "#A4262C",
        11 => "#486860", 12 => "#2B3A42", 13 => "#7A7574", 14 => "#5D5A58", 15 => "#000000",
        16 => "#750B1C", 17 => "#CA5010", 18 => "#C19C00", 19 => "#986F0B", 20 => "#0B6A0B",
        21 => "#038387", 22 => "#6B8E23", 23 => "#004E8C", 24 => "#32145A", 25 => "#5C0E1E",
        _ => null
    };

    private sealed record CalendarLoadResult(IReadOnlyList<OutlookAppointmentData> Appointments, bool HadFailures);
#endif
}
