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
PromptVariableTests.Run();
OpenApiToolDiscoveryTests.Run();
await LocalStorageAndWorkspaceTests.RunAsync();
ExternalLinkTests.Run();
OperationJournalTests.Run();
OperationServiceTests.Run();
WorkflowServiceTests.Run();
WorkflowRunServiceTests.Run();
WorkflowApprovalTimingTests.Run();
WorkflowCapabilityGateTests.Run();
ReadOnlyWorkspaceAdapterTests.Run();
ChatMessageQueueTests.Run();
ChatMessageQueueStoreTests.Run();
ConversationSearchTests.Run();
ScheduledTaskTests.Run();
NavigationHistoryTests.Run();
ModelComparisonPolicyTests.Run();
SkillPackageInstallerTests.Run();
McpToolPermissionGateTests.Run();
await McpStdioClientTests.RunAsync();
McpConnectionRegistryTests.Run();
await McpStreamableHttpClientTests.RunAsync();
await ReleaseManifestVerifierTests.RunAsync();
ConversationContextPlannerTests.Run();
MemoryStoreTests.Run();
StatusMessageHistoryTests.Run();
MemoryPromptContextTests.Run();

Console.WriteLine("Native core checks passed.");
