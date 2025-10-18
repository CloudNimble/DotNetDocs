\# DocsJsonManager Use Cases



\## Case 1: Starting From Nothing



The DocsJsonManager should give you an amazing way to build out docs.json files from scratch. The built-in defaults should give you an idiot-proof way to create Minimum-Viable Documentation.



\## Case 2: Starting from a Template



Inside a .docsproj Project, you can set up a MintlifyTemplate that has your baseline documentation setup. The SDK will turn that XML into a DocsJsonConfig object. That object will get passed into the `DocsJsonManager.Load(DocsJsonConfig)` method to be the foundation from which the rest of a folder or generated C# documentation is processed.



In this case, as with the remaining cases, the DocsJsonManager will be loaded up with existing paths, and those paths will have to be tracked and checked as new navigation elements are added to the Configuration.Navigation.



\## Case 3: Starting from an existing docs.json



You should be able to load up an existing docs.json file and validate it, change settings, merge it with something else, and save it, just like the other use cases.



\## Case 4: Combining Multiple Documentations



If you are documenting a system that contains multiple disparate open-source projects (like what we're doing with htts://easyaf.dev), then we should eb able to load multiple separately-compiled projects together into a single docs.json file, where each project is a Tab across the top of the site.



\## Edge Cases



\- If an DocsJsonConfig or docs.json file is loaded and it has duplicate paths in the navigation, the system should assume that the end-user made a deliberate choice to have the page in there more than once, and not block it being added to the C# object model. 

&nbsp; - This behavior should be overridden in ProjectContext where AllowDuplicatePathss is turned off, and maybe the Load methods should have an optional allowDuplicatePaths parameter that defaults to true.

