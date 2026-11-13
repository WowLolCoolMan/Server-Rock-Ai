// Conversation branches and forks.
// A saved chat can hold alternate histories (ChatSession.Branches). BranchIndex 0 is the main
// history (ChatSession.Messages); 1..N pick Branches[index - 1]. The ‹ › header buttons move
// between them. ForkSession copies the visible history into a brand-new chat instead.
// Depends on: SaveCurrentSession / PersistConversationEdits / RenderRecent (MainPage.Recents.cs),
// RebuildConversationView, and the Branch* elements in MainPage.xaml.
// Called by: the branch buttons in MainPage.xaml, opening a saved chat, the recent-chat "Fork"
// action, and UpdateModeControls.

using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using ServerRock.Core.Models;
using ServerRock.Core.Services;

namespace ServerRock.Native;

public sealed partial class MainPage
{
    private IReadOnlyList<ChatMessage> SelectedBranchMessages(ChatSession session)
    {
        if (session.BranchIndex > 0 && session.BranchIndex <= session.Branches.Count)
            return session.Branches[session.BranchIndex - 1];
        return session.Messages;
    }

    private void UpdateBranchControls()
    {
        var session = _activeSessionId is null ? null : _sessions.FirstOrDefault(item => item.Id == _activeSessionId);
        var hasBranches = session is not null && session.Branches.Count > 0;
        BranchControls.Visibility = hasBranches ? Visibility.Visible : Visibility.Collapsed;
        if (!hasBranches || session is null) return;
        var index = Math.Clamp(session.BranchIndex, 0, session.Branches.Count);
        var position = index + 1;
        BranchPositionText.Text = index == 0 ? $"Main · {position} / {session.Branches.Count + 1}" : $"{position} / {session.Branches.Count + 1}";
        PreviousBranchButton.IsEnabled = index > 0;
        NextBranchButton.IsEnabled = index < session.Branches.Count;
        AutomationProperties.SetName(BranchPositionText, index == 0
            ? $"Main conversation branch, {position} of {session.Branches.Count + 1}"
            : $"Conversation branch {position} of {session.Branches.Count + 1}");
    }

    private void PreviousBranchClick(object sender, RoutedEventArgs e) => SelectConversationBranch(-1);
    private void NextBranchClick(object sender, RoutedEventArgs e) => SelectConversationBranch(1);

    private void SelectConversationBranch(int delta)
    {
        if (_requestCancellation is not null) { RuntimeStatus.Text = "Wait for the current response to finish before changing branches."; return; }
        if (_activeSessionId is null) return;
        var session = _sessions.FirstOrDefault(item => item.Id == _activeSessionId);
        if (session is null || session.Branches.Count == 0) return;
        SaveCurrentSession();
        var current = Math.Clamp(session.BranchIndex, 0, session.Branches.Count);
        var selected = Math.Clamp(current + delta, 0, session.Branches.Count);
        session.BranchIndex = selected;
        _messages.Clear();
        _messages.AddRange(SelectedBranchMessages(session));
        if (!PersistConversationEdits()) { session.BranchIndex = current; _messages.Clear(); _messages.AddRange(SelectedBranchMessages(session)); return; }
        RebuildConversationView();
        UpdateBranchControls();
        RuntimeStatus.Text = $"Showing conversation branch {selected + 1} of {session.Branches.Count + 1}.";
    }

    private void ForkSession(string sessionId, int? throughMessageIndex = null)
    {
        if (_requestCancellation is not null) { RuntimeStatus.Text = "Wait for the current response to finish before creating a fork."; return; }
        var source = _sessions.FirstOrDefault(item => item.Id == sessionId);
        if (source is null) return;
        var sourceMessages = _activeSessionId == sessionId ? _messages : SelectedBranchMessages(source);
        if (sourceMessages.Count == 0) { RuntimeStatus.Text = "Send at least one message before creating a fork."; return; }
        if (_activeSessionId == sessionId) SaveCurrentSession();
        IReadOnlyList<ChatMessage> forkMessages;
        try { forkMessages = ConversationForkService.CopyMessages(sourceMessages, throughMessageIndex); }
        catch (ArgumentOutOfRangeException) { RuntimeStatus.Text = "That message is no longer available to fork."; return; }
        var fork = new ChatSession
        {
            Title = throughMessageIndex is int messageIndex ? $"{source.Title} · fork from message {messageIndex + 1}" : $"{source.Title} · fork",
            Mode = source.Mode,
            ProjectId = source.ProjectId,
            ProjectName = source.ProjectName,
            ProjectInstructions = source.ProjectInstructions,
            IncludeProjectFilesInChat = source.IncludeProjectFilesInChat,
            ForkedFrom = source.Id,
            Messages = [.. forkMessages]
        };
        _sessions.Add(fork);
        if (!PersistConversationEdits()) { _sessions.Remove(fork); return; }
        if (_activeSessionId != sessionId)
        {
            RenderRecent();
            RuntimeStatus.Text = "Fork created from the selected chat.";
            return;
        }
        _activeSessionId = fork.Id;
        _activeChatFolder = null;
        _messages.Clear();
        _messages.AddRange(fork.Messages);
        PageTitle.Text = fork.Title;
        ChatFolderBadge.Visibility = Visibility.Collapsed;
        RenderRecent();
        RebuildConversationView();
        UpdateBranchControls();
        RuntimeStatus.Text = throughMessageIndex is int selectedIndex
            ? $"Fork created from message {selectedIndex + 1}; the original conversation is unchanged."
            : "Fork created from the selected conversation branch.";
    }

    private void ForkFromMessageClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: int messageIndex } || _activeSessionId is null) return;
        ForkSession(_activeSessionId, messageIndex);
    }
}
