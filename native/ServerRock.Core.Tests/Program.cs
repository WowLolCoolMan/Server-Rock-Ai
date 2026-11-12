// Core test runner. Each area lives in its own *Tests.cs file; they run in this order
// and the run stops at the first failed Check (see TestSupport.cs).

await DiagnosticsTests.RunAsync();
CatalogTests.Run();
await ProjectHealthTests.RunAsync();
await ModelLibraryTests.RunAsync();
await ChatProviderTests.RunAsync();
await ImageGenerationTests.RunAsync();
await AttachmentTests.RunAsync();
await BoundedReadTests.RunAsync();
await ConversationArchiveTests.RunAsync();
await WebSearchTests.RunAsync();
await GitHubTests.RunAsync();
ChatMessageTests.Run();
await LocalStorageAndWorkspaceTests.RunAsync();
ExternalLinkTests.Run();
OperationJournalTests.Run();
OperationServiceTests.Run();
WorkflowServiceTests.Run();
WorkflowRunServiceTests.Run();
WorkflowCapabilityGateTests.Run();

Console.WriteLine("Native core checks passed.");
