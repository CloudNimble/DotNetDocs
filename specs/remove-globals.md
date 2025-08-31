# Plan to Remove IgnoreGlobalModule Support

## Overview
Remove all support for the `IgnoreGlobalModule` property and always exclude the `<Module>` type from documentation output, as it provides no value for API documentation.

## Implementation Checklist

### Core Changes

- [ ] **ProjectContext.cs**
  - [ ] Remove the `IgnoreGlobalModule` property entirely

- [ ] **AssemblyManager.cs**
  - [ ] Remove the `ignoreGlobalModule` parameter from `BuildModel()` method
  - [ ] Remove the `ignoreGlobalModule` parameter from `ProcessNamespace()` method
  - [ ] Simplify `ProcessNamespace()` to always skip the global namespace's `<Module>` type
  - [ ] Remove debug logging added for module detection

- [ ] **DocumentAsync() calls**
  - [ ] Update all calls to `BuildModel()` to remove the `ignoreGlobalModule` parameter

### Test Updates

- [ ] **AssemblyManagerTests.cs**
  - [ ] Remove the DataRow attributes testing with/without globals
  - [ ] Simplify `DocumentAsync_ProducesConsistentBaseline` to single test
  - [ ] Remove `GenerateAssemblyBaseline` code for WithGlobals variant
  - [ ] Delete the second baseline file generation

- [ ] **DotNetDocsTestBase.cs**
  - [ ] Remove `ignoreGlobalModule` parameter from `GetTestsDotSharedAssembly()` method
  - [ ] Update method to not use ProjectContext with this property

- [ ] **JsonRendererTests.cs**
  - [ ] Remove DataRow testing for `ignoreGlobalModule`
  - [ ] Simplify `SerializeNamespaces_Should_Return_Anonymous_Object_Collection` test

- [ ] **MarkdownRendererTests.cs**
  - [ ] Remove DataRow testing for `ignoreGlobalModule`
  - [ ] Simplify `RenderAssemblyAsync_Should_List_Namespaces` test

- [ ] **YamlRendererTests.cs**
  - [ ] Remove DataRow testing for `ignoreGlobalModule`
  - [ ] Simplify `SerializeNamespaces_Should_Return_List_Of_Namespace_Dictionaries` test

### File Cleanup

- [ ] **Delete Files**
  - [ ] ModuleInit.cs - No longer needed since we're not testing module detection
  - [ ] CheckModule.cs - Temporary test file created during investigation
  - [ ] test-module.csx - Temporary script file created during investigation
  - [ ] BasicAssembly_WithGlobals.json baseline file

### Final Steps

- [ ] Rebuild the solution
- [ ] Regenerate all baselines using `dotnet breakdance generate`
- [ ] Run all tests to ensure they pass

## Expected Outcome
- Cleaner, simpler code without unnecessary complexity
- Single baseline file for assembly tests
- All tests passing with simplified logic
- No documentation of compiler-generated `<Module>` types