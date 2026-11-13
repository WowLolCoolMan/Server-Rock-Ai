using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using SkaiDesktop.Core.Models;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private async Task ProcessDueScheduledTaskAsync()
    {
        ScheduledTask? task;
        try { task = _scheduledTaskStore.TryClaimDue(); }
        catch (Exception ex)
        {
            RuntimeStatus.Text = $"Scheduled tasks could not be checked: {ex.Message}";
            return;
        }
        if (task is null) return;

        try
        {
            if (_requestCancellation is not null)
            {
                _messageQueue.Enqueue(task.Prompt);
                PersistMessageQueue();
                UpdateQueueControl();
                _scheduledTaskStore.Complete(task.Id, true, "Queued for the active response to finish.");
                RuntimeStatus.Text = $"Scheduled task queued: {task.Prompt}";
            }
            else
            {
                PromptBox.Text = task.Prompt;
                PromptBox.Focus(FocusState.Programmatic);
                PromptBox.SelectionStart = PromptBox.Text.Length;
                _scheduledTaskStore.Complete(task.Id, true, "Surfaced in the composer for review; no provider request was sent.");
                RuntimeStatus.Text = "A scheduled task is ready in the composer. Review it before sending.";
            }
        }
        catch (Exception ex)
        {
            try { _scheduledTaskStore.Complete(task.Id, false, $"Could not surface task: {ex.Message}"); }
            catch (Exception completionError) { App.Log.Error("Scheduler", completionError, "Could not record scheduled task failure"); }
            RuntimeStatus.Text = $"Scheduled task could not run: {ex.Message}";
        }
    }

    private async Task CreateScheduledTaskAsync(ContentDialog? activityDialog)
    {
        var prompt = new TextBox { Header = "Prompt", AcceptsReturn = true, TextWrapping = TextWrapping.Wrap, MinHeight = 92, MaxLength = 50_000, PlaceholderText = "What should be ready for review later?" };
        AutomationProperties.SetName(prompt, "Scheduled task prompt");
        var dueDate = new DatePicker { Header = "Date", Date = DateTimeOffset.Now.Date };
        var dueTime = new TimePicker { Header = "Time", Time = DateTimeOffset.Now.AddMinutes(5).TimeOfDay }; 
        AutomationProperties.SetName(dueDate, "Scheduled task date");
        AutomationProperties.SetName(dueTime, "Scheduled task time");
        var mode = new ComboBox { Header = "Workspace", MinWidth = 220 };
        mode.Items.Add("Chat");
        mode.Items.Add("Coding Agent");
        mode.SelectedIndex = _mode == WorkspaceModes.CodingAgent ? 1 : 0;
        AutomationProperties.SetName(mode, "Scheduled task workspace");
        var repeat = new ComboBox { Header = "Repeat", MinWidth = 220 };
        repeat.Items.Add("One-time");
        repeat.Items.Add("Every hour");
        repeat.Items.Add("Every 2 hours");
        repeat.Items.Add("Daily");
        repeat.Items.Add("Weekly");
        repeat.SelectedIndex = 0;
        AutomationProperties.SetName(repeat, "Scheduled task recurrence");
        var note = new TextBlock { Text = "When due, the prompt is placed in the composer for review. Recurring tasks reschedule only after a successful surfaced run; failures stop for review. Normal model availability, remote-data consent, and send controls still apply.", TextWrapping = TextWrapping.Wrap, FontSize = 11, Opacity = 0.78 };
        var content = new StackPanel { Spacing = 9, Children = { prompt, dueDate, dueTime, mode, repeat, note } };
        var dialog = new ContentDialog { Title = "Schedule task", Content = content, PrimaryButtonText = "Schedule", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
        dialog.Opened += (_, _) => prompt.Focus(FocusState.Programmatic);
        activityDialog?.Hide();
        if (await dialog.ShowAsync() != ContentDialogResult.Primary) return;
        if (string.IsNullOrWhiteSpace(prompt.Text))
        {
            RuntimeStatus.Text = "Enter a prompt before scheduling a task.";
            return;
        }
        var localDate = dueDate.Date.Date;
        var localDateTime = localDate + dueTime.Time;
        var dueAtUtc = new DateTimeOffset(localDateTime, TimeZoneInfo.Local.GetUtcOffset(localDateTime)).ToUniversalTime();
        try
        {
            var repeatEveryMinutes = repeat.SelectedIndex switch { 1 => 60, 2 => 120, 3 => 24 * 60, 4 => 7 * 24 * 60, _ => (int?)null };
            var scheduled = _scheduledTaskStore.Schedule(prompt.Text, dueAtUtc, mode.SelectedIndex == 1 ? WorkspaceModes.CodingAgent : WorkspaceModes.Chat, repeatEveryMinutes: repeatEveryMinutes);
            RuntimeStatus.Text = $"Scheduled task for {scheduled.DueAtUtc.ToLocalTime():g}{(repeatEveryMinutes is null ? string.Empty : $" · repeats every {repeatEveryMinutes} minutes")}. It will appear in the composer for review.";
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Task could not be scheduled: {ex.Message}"; }
    }

    private async Task ChangeScheduledTaskAsync(ScheduledTask task, bool resume, ContentDialog? activityDialog)
    {
        try
        {
            if (resume) _scheduledTaskStore.Resume(task.Id);
            else _scheduledTaskStore.Pause(task.Id);
            RuntimeStatus.Text = resume ? "Scheduled task resumed." : "Scheduled task paused.";
            activityDialog?.Hide();
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Scheduled task could not be changed: {ex.Message}"; }
        await Task.CompletedTask;
    }

    private async Task CancelScheduledTaskAsync(ScheduledTask task, ContentDialog? activityDialog)
    {
        try
        {
            _scheduledTaskStore.Cancel(task.Id);
            RuntimeStatus.Text = "Scheduled task cancelled locally before execution. No prompt was sent and no provider was called.";
            activityDialog?.Hide();
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Scheduled task could not be cancelled: {ex.Message}"; }
        await Task.CompletedTask;
    }

    private async Task RunScheduledTaskNowAsync(ScheduledTask task, ContentDialog? activityDialog)
    {
        try
        {
            _scheduledTaskStore.RunNow(task.Id);
            activityDialog?.Hide();
            await ProcessDueScheduledTaskAsync();
        }
        catch (Exception ex) { RuntimeStatus.Text = $"Scheduled task could not be run now: {ex.Message}"; }
    }
}
