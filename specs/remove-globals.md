# Plan to Remove IgnoreGlobalModule Support ✅ COMPLETED

## Overview
Remove all support for the `IgnoreGlobalModule` property and always exclude the `<Module>` type from documentation output, as it provides no value for API documentation.

## Implementation Checklist

### Core Changes

- [x] **ProjectContext.cs**
  - [x] Remove the `IgnoreGlobalModule` property entirely

- [x] **AssemblyManager.cs**
  - [x] Remove the `ignoreGlobalModule` parameter from `BuildModel()` method
  - [x] Remove the `ignoreGlobalModule` parameter from `ProcessNamespace()` method
  - [x] Simplify `ProcessNamespace()` to always skip the global namespace's `<Module>` type
  - [x] Remove debug logging added for module detection

- [x] **DocumentAsync() calls**
  - [x] Update all calls to `BuildModel()` to remove the `ignoreGlobalModule` parameter

### Test Updates

- [x] **AssemblyManagerTests.cs**
  - [x] Remove the DataRow attributes testing with/without globals
  - [x] Simplify `DocumentAsync_ProducesConsistentBaseline` to single test
  - [x] Remove `GenerateAssemblyBaseline` code for WithGlobals variant
  - [x] Delete the second baseline file generation

- [x] **DotNetDocsTestBase.cs**
  - [x] Remove `ignoreGlobalModule` parameter from `GetTestsDotSharedAssembly()` method
  - [x] Update method to not use ProjectContext with this property

- [x] **JsonRendererTests.cs**
  - [x] Remove DataRow testing for `ignoreGlobalModule`
  - [x] Simplify `SerializeNamespaces_Should_Return_Anonymous_Object_Collection` test

- [x] **MarkdownRendererTests.cs**
  - [x] Remove DataRow testing for `ignoreGlobalModule`
  - [x] Simplify `RenderAssemblyAsync_Should_List_Namespaces` test

- [x] **YamlRendererTests.cs**
  - [x] Remove DataRow testing for `ignoreGlobalModule`
  - [x] Simplify `SerializeNamespaces_Should_Return_List_Of_Namespace_Dictionaries` test

### File Cleanup

- [x] **Delete Files**
  - [x] ModuleInit.cs - No longer needed since we're not testing module detection
  - [x] CheckModule.cs - Temporary test file created during investigation
  - [x] test-module.csx - Temporary script file created during investigation
  - [x] BasicAssembly_WithGlobals.json baseline file

### Final Steps

- [x] Rebuild the solution
- [x] Regenerate all baselines using `dotnet breakdance generate`
- [x] Run all tests to ensure they pass

## Expected Outcome ✅ ACHIEVED
- Cleaner, simpler code without unnecessary complexity
- Single baseline file for assembly tests
- All tests passing with simplified logic
- No documentation of compiler-generated `<Module>` types

## Completion Notes
All tasks in this plan have been successfully completed:
- The `IgnoreGlobalModule` property has been completely removed from the codebase
- All test methods have been simplified to remove DataRow attributes and dual baselines
- The BasicAssembly_WithGlobals.json baseline file has been deleted
- All temporary test files (ModuleInit.cs, CheckModule.cs, test-module.csx) have been removed
- The global namespace handling is now implicit - the code processes it but sets Name to empty string for global namespaces
- All references to `ignoreGlobalModule` have been eliminated from the entire codebase