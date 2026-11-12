using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using ServerRock.Core.Models;

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
        var note = new TextBlock { Text = "When due, the prompt is placed in the composer for review. Normal model availability, remote-data consent, and send controls still apply.", TextWrapping = TextWrapping.Wrap, FontSize = 11, Opacity = 0.78 };
        var content = new StackPanel { Spacing = 9, Children = { prompt, dueDate, dueTime, mode, note } };
        var dialog = new ContentDialog { Title = "Schedule one-time task", Content = content, PrimaryButtonText = "Schedule", CloseButtonText = "Cancel", XamlRoot = XamlRoot };
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
            var scheduled = _scheduledTaskStore.Schedule(prompt.Text, dueAtUtc, mode.SelectedIndex == 1 ? WorkspaceModes.CodingAgent : WorkspaceModes.Chat);
            RuntimeStatus.Text = $"Scheduled task for {scheduled.DueAtUtc.ToLocalTime():g}. It will appear in the composer for review.";
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
}
