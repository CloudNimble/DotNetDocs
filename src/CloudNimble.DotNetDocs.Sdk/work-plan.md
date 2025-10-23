* Improve current SDK implementation
  * improve discovery Task/logic
    * don't do eval in DiscoverDocumentedProjectsTask
      * instead use Target calls
    * ensure that referenced projects are built before documentation generation
    * incrementality
    * add a placeholder for multi-tfm handling
  * improve sdk packaging
    * use nuget's per-tfm capabilities to do the current Task packaging
    * remove need for separate project
    * ensure deps are bundled per-TFM as well for correct loading
    * remove use of pinned-per-tfm extensions deps - newer versions are back-compatible
  * improve local use of custom SDK
    * add explicit Imports to local samples for Sdk.props and Sdk.targets to remove need to build entire package and manage nuget package versions
