* Improve current SDK implementation
  * improve discovery Task/logic
    * don't do eval in DiscoverDocumentedProjectsTask x
      * instead use Target calls x
    * ensure that referenced projects are built before documentation generation x
    * incrementality
    * add a placeholder for multi-tfm handling 
  * improve sdk packaging x
    * use nuget's per-tfm capabilities to do the current Task packaging x
    * remove need for separate project x
    * ensure deps are bundled per-TFM as well for correct loading x
    * remove use of pinned-per-tfm extensions deps - newer versions are back-compatible x
  * improve local use of custom SDK
    * add explicit Imports to local samples for Sdk.props and Sdk.targets to remove need to build entire package and manage nuget package versions
