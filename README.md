This repository contains the Hololens 2 version of the DatAR project, an experimental virtual environment for the exploration of neuroscientific literature. The code in this project is a ported version of [the original DatAR repository](https://git.science.uu.nl/g.tanhaei/datar) in order to move away from SteamVR and instead support Microsoft Mixed Reality Toolkit. For access and more information, contact [Ghazaleh Tanhei](mailto:g.tanhaei@uu.nl).

*Last updated 10-09-2021*
*Unity version 2020.3.15f2*

## Table of Contents
- [Table of Contents](#table-of-contents)
- [Contributors Guide](#contributors-guide)
- [Scenes](#scenes)
- [Building blocks](#building-blocks)
- [Widgets](#widgets)
  * [Query widgets](#query-widgets)
  * [Manipulation widgets](#manipulation-widgets)
  * [Visualisation widgets](#visualisation-widgets)
  * [Export widgets](#export-widgets)
- [Data Models](#data-models)
  * [Dynamic resources](#dynamic-resources)
  * [Passables (or, statically-typed resources)](#passables--or--statically-typed-resources-)
  * [JSON context](#json-context)
- [Auxiliary](#auxiliary)
- [DatAR Web Application - Hololens 2 Edition](#datar-web-application---hololens-2-edition)
- [Known issues](#known-issues)

## Contributors Guide
If you want to contribute to the project, this section will introduce you to our main design decisions and should get you up to speed in making your own adjustments. We will go over the data models underpinning the environment, how such data is represented visuospatially, and how widgets function to query, manipulate or export data.

All DatAR-related assets can be found in the `DatAR` folder; all other code is by third parties. Code is divided by concern rather than by file type. We will go over each top folder in detail. You are advised to keep the complete file structure close to you when going through this guide.

```markdown
DatAR
|- Auxiliary
|      Supporting materials of any kind; mainly shared resources and singletons.
|- BuildingBlocks
|      Re-usable design patterns for the development of new widgets.
|- DataModels
|      Classes that structure data of any kind in the application.
|- Scenes
|      Sample scenes to get you started.
|- Widgets
|      Ready-made analytics tools to use in the environment.
```

## Scenes
The `Scenes` folder contains a demonstration scene (`Playground`) and an empty scene to start from scratch with all necessary singletons and MRTK settings (`Starter`).

## Building blocks
The `BuildingBlocks` folder contains ready-to-use design patterns, among which the chief representation in DatAR: the **Resource Sphere (RS)**.  RSs are manifestations of resources that can be moved through the virtual space. Examples would be the concepts Amygdala and Depression, and the class Disease. These representations act as input- and output of widgets, through **Receptacles (input)** and **RS Manufacturers (output)**. Another core representation is the **Dataflow**, which transfer lists of Concepts between Widgets (using an **Inlet** and an **Outlet**).

To create a new Widget, start by dragging a `Projector` prefab into the scene. This is the wrapper of your Widget, and allows users to move them around. You might want to add an icon at this stage, for easy recognition. You can then add additional prefabs based on needs. For input, you can add one or more `Receptacle` or `DataflowInlet` prefabs. You can listen to these in your main script using the BehaviorSubject fields that these prefabs/components expose. For output, you can add a `ResourceSphereManufacturer` or `DataflowOutlet`. You can send these prefabs/components data once your widget finished processing. One final building block is the `QueryStatusIndicator`. This prefab can listen to your Widget class if you implement the `IQueryState` interface. Its primary function is to display progress to users.

Implementation-wise, communication between building blocks uses a reactive framework, *[UniRx](https://github.com/neuecc/UniRx)* and *[UniTask](https://github.com/Cysharp/UniTask)*. This allows intuitive live-processing of changes in data based on user input or system updates. You are recommended to look at pre-implemented Widgets to see how all these elements fit together.

## Widgets
The `Widgets` folder contains pre-made tools that the user can use in the analytics environment. There are several different types of widgets available, indicated by the first word in their name.

Widgets require different **inputs** depending on their function. They can be standalone, requiring no further user input like the `BrainModel` widget. Other widgets have **receptacles**, in which one or more RSs need to be placed. Restrictions can apply to which types of resources are placed (e.g., one concept and one class in the `QueryCooccurrences` widget). Finally, some widgets allow or require a **dataflow** rather than a RSs to be connected; these can be connected using outlets and inlets (where each outlet can connect to multiple inlets). The inlet is the one managing the connection.

All widgets are explained in the following sections. An icon is added for the following conditions:

- Whether Widgets require pulling from or pushing to external sources (🌐) or not (📵).
- If a Widget uses one or more Inlets (🟪) or outlet (🟦), Receptacles (🔵) or RS Manufacturers (🔴).
- If visual output is generated (🖌️).

### Query widgets

Query widgets retrieve primary data from a SPARQL endpoint. The query can be simple in nature, such as retrieving all available classes at a given endpoint, or it can be complex and yield multi-item data structures (such as co-occurrences).

- **QueryAvailableClasses** 🌐→🔴

    ***Input:***

    *none*

    ***Output:***

    RSs: All Classes available. Data retrieved from triple store.

- **QueryConceptsOfClass** 🌐🔵→🔴

    ***Input:***

    Receptacle: RS of type Class

    ***Output:***

    RSs: All Concepts of the given Class. Data retrieved from triple store. 

- **QueryCooccurrences** 🌐🔵🔵→🟦

    ***Input:***

    Receptacle: RS of type Class

    Receptacle: RS of type Concept

    ***Output:***

    Outlet: Dataflow of co-occurrences (containing all concepts in the given Class that cooccurred with the given Concept). Data retrieved from triple store. This output also includes co-occurrence counts and two-way probabilities of a co-occurrence in the literature (see Data Models section).

### Manipulation widgets

Manipulation widgets take in a resource or dataflow and change the data. This can be according to user parameters or fully automatic. An example is the data filter widget.

- **ManipulationMinMaxFilter** 📵🟪→🟦

    ***Input:***

    Inlet: Dataflow of co-occurrences.

    ***Output:***

    Outlet: Dataflow of co-occurrences, where filtered items receive a flag. This flag can be read by other widgets to display three states: not in data set, filtered-in, and filtered-out.

### Visualisation widgets

Visualisation widgets take in a resource or dataflow and show its contents to the user. It can do so through text or by using 2D or 3D visualisations.

- **VisualisationInspectorResourceSphere** 🌐🔵→🔴🖌️

    ***Input:*** 

    Receptacle: RS of any type.

    ***Output:***

    Visualisation: Descriptions pulled from external sources (MESH, WikiData); raw data if in debug mode.

    RSs: All Concepts that closely match the given Concept.

- **VisualisationInspectorDataflow** 📵🟪→🖌️

    ***Input:***

    Inlet: Dataflow of co-occurrences.

    ***Output:***

    Visualisation: Rendered List of co-occurrences, with names of each pair of co-occurring concepts and their absolute count of being mentioned together.

- **VisualisationTopicModel** 🌐🔵🟪→🖌️

    ***Input:***

    Receptacle: RS of type Class

    Inlet: Dataflow of co-occurrences. Optional, and used to display filter states.

    ***Output:***

    Visualisation: Topic model of all items in given Class. Data is stored on a triple store (currently only contains Diseases).

- **VisualisationBrainModel** 🌐🟪→🖌️

    ***Input:***

    Inlet: Dataflow of co-occurrences. Optional, and used to display filter states.

    ***Output:***

    Visualisation: Brain regions' central points displayed as a scatterplot. Data is stored on a triple store.

 - **VisualisationDiseaseRelations** 🌐🔵🔵→🖌️
    
    ***Input:***
    
    Receptacle: RS of type Disease
    
    Receptacle: RS of type Disease
    
    ***Output:***
    
    Visualisation: A visual representation of the co-occurrences between the two diseases and any brain related concept.

### Export widgets

Export widgets move data from the current environment to another environment. In our prototype, it can send concept pairs to DatAR Web for further sentence-level analysis in academic papers.

- **ExportConceptPair** 🔵🔵→🌐

    > Note: This currently does not work for the Hololens 2 version. Instead, 5 sentences containing co-occurrences are displayed in the application itself.

    ***Input:***

    Receptacle: RS of type Region

    Receptacle: RS of type Disease

    ***Output:***

    Export: Data is sent to message broker to be received by any environments on the same channel. In our case, if you have DatAR Web open, it will receive it. No additional data is pulled from external sources within Unity.
## Data Models
This `DataModels` folder contains classes that structure all data use in the DatAR environment. Widgets (and their building blocks) use these data structures under the hood. The internal data structure follows the guidelines of the [JSON-LD standard](https://json-ld.org/), and is easily exported as such. This allows interoperability with other (linked data) systems. 

We distinguish between several data types. At the root of the class hierarchy is the ***Resource*** class. This class only requires an assigned ID. Other data structures inherit from it and expand on required data fields.

### Dynamic resources
Generally, we cannot assume type information from linked data sources (following the [open-world principle](https://en.wikipedia.org/wiki/Open-world_assumption)). External data therefore stores its type information as an attribute of a generic class rather than be managed by the C# type system. In order to allow type-inferred behaviour, we use *Passables* instead (introduced next).

***A full example of a generic dynamic resource:***

```json
{
  "@id":  "lbd:amygdala",
  "@type":  ["lbd:region"],
  "rdfs:label":  "amygdala"
}
```

### Passables (or, statically-typed resources)

A **passable** is a strongly typed data entity that wraps one or more dynamic resources. Its `@type` value allows widgets to infer if and how they can display this data type. Basic types include:

- `datar:ConceptPassable` (which could contain, e.g., `lbd:amygdala`)
- `datar:ClassPassable` (which could contain, e.g., `lbd:Brain_Region`)
- `datar:ConceptListPassable` (which would contain multiple concepts)
- `datar:ClassListPassable` (which would contain multiple classes)

A more involved type is `datar:CooccurrenceListPassable`, which contains a custom `datar:resources`, `datar:concept` and `datar:class` that describe a cooccurrence. These more complex schema require tailored linked data queries. You can create your own schema, create a query widget to generate items with that schema, and build new widgets or extend existing ones to support your schema. For example, you could extend the Min-Max Filter to support any numerical data it receives.

***A full example of a Passable of the above dynamic resource***

*Note that the ID is the same as the contained resource. When loaded into DatAR, C# infers its meta-type (*`datar:ConceptPassable`)*, allowing easy access to any of its properties.*
*Note that a resource can belong to more than one type, primarily in the case of brain concepts. The @type parameter is therefore of an IEnumerable type.*

```json
{
  "@id": "lbd:amygdala",
  "@type": ["datar:ConceptPassable"],
  "datar:resource": {
    "@id": "lbd:amygdala",
    "@type": ["lbd:region"],
	"rdfs:label": "Amygdala"
  }
}
```

***An example of a custom passable for co-occurrences***

*Note that the ID is a hash of the contents of the `datar:resources` array; this is the case for all list passables.*

```json
{
  "data": {
    "@id": "_:155fec387b2589cd0ad262fdcde2b422d5bd1f734712d0b43c149208b585433e",
    "datar:class": {
      "@id": "lbd:disease",
      "@type": ["rdf:Class"],
      "rdfs:label": "disease"
    },
    "datar:concept": {
      "@id": "lbd:amygdala",
      "@type": ["lbd:region"],
      "rdfs:label": "amygdala"
    },
    "datar:resourceTypes": [
      "datar:CooccurrenceStatement"
    ],
    "datar:resources": [
      {
        "@id": "lbd:amygdala2alzheimers_disease",
        "@type": ["datar:CooccurrenceStatement"],
        "datar:classItem": {
          "@id": "lbd:alzheimers_disease",
          "@type": ["lbd:disease"],
          "rdfs:label": "alzheimers disease"
        },
        "datar:classItemMentionedWhenConceptItemMentioned": 0.058339962874569079,
        "datar:conceptMentionedWhenClassItemMentioned": 0.015423443634324173,
        "datar:cooccurrences": 220,
        "datar:filterSelectionState": 1,
        "rdfs:label": "amygdala2alzheimers disease"
      },
      {
        "@id": "lbd:amygdala2anxiety",
        "@type": ["datar:CooccurrenceStatement"],
        "datar:classItem": {
          "@id": "lbd:anxiety",
          "@type": ["lbd:disease"],
          "rdfs:label": "anxiety"
        },
        "datar:classItemMentionedWhenConceptItemMentioned": 0.4237602757889154,
        "datar:conceptMentionedWhenClassItemMentioned": 0.23441396508728179,
        "datar:cooccurrences": 1598,
        "datar:filterSelectionState": 1,
        "rdfs:label": "amygdala2anxiety"
      },
      {
        "@id": "lbd:amygdala2atherosclerosis",
        "@type": ["datar:CooccurrenceStatement"],
        "datar:classItem": {
          "@id": "lbd:atherosclerosis",
          "@type": ["lbd:disease"],
          "rdfs:label": "atherosclerosis"
        },
        "datar:classItemMentionedWhenConceptItemMentioned": 0.00053036329885971893,
        "datar:conceptMentionedWhenClassItemMentioned": 0.0011129660545353367,
        "datar:cooccurrences": 2,
        "datar:filterSelectionState": 1,
        "rdfs:label": "amygdala2atherosclerosis"
      }
    ]
  }
}
```

### JSON context

The following linked data context is used inside of the application, which allow using the compact form (you can play with it in the [JSON-LD playground](https://json-ld.org/playground/#)):

```json
{ 
  "@context": {
    "xsd": "http://www.w3.org/2001/XMLSchema#",
    "lbd": "http://www.linked-neuron-data.org/resource/",
    "lbdo": "http://www.linked-neuron-data.org/ongoloty/",
    "lbdp": "http://www.linked-neuron-data.org/property/",
    "lbdg": "http://www.linked-neuron-data.org/graph/",
    "rdf": "http://www.w3.org/1999/02/22-rdf-syntax-ns#",
    "rdfs": "http://www.w3.org/2000/01/rdf-schema#",
    "meshv": "http://id.nlm.nih.gov/mesh/vocab#",
    "mesh": "http://id.nlm.nih.gov/mesh/",
    "mesh2020": "http://id.nlm.nih.gov/mesh/2020/",
    "mesh2019": "http://id.nlm.nih.gov/mesh/2019/",
    "mesh2018": "http://id.nlm.nih.gov/mesh/2018/",
    "wd": "http://www.wikidata.org/entity/",
    "schema": "http://schema.org/",
    "schema:description": { "@language": "en" },
    "datar": "https://datar.local/ontology/",
    "datar:cooccurrences": { "@type": "xsd:int" },
    "datar:coordX": { "@type": "xsd:float" },
    "datar:coordY": { "@type": "xsd:float" },
    "datar:coordZ": { "@type": "xsd:float" },
    "datar:appearTimes": { "@type": "xsd:int" }
  }
}
```

## Auxiliary

The `Auxiliary` folder contains supporting materials, among which icons, images, materials, and meshes. It also contains scripts for use in the editor (`EditorScripts`), scripts used in multiple building blocks (`SharedScripts`), any MRTK extension scripts and templates to set environment variables (`ScriptableObjects`). 

Singletons are also placed here, in the `Services` folder. These are classes that are part of the Services Prefab, which should be in any scene that uses DatAR building blocks. Other GameObjects can look for this object by name.

Finally, there are several supporting prefabs in the `Prefabs` folder. The Entity Pools need to be in every scene (one of each); others can be placed at will where useful.

## DatAR Web Application - Hololens 2 Edition

The [DatAR Web Application](https://git.science.uu.nl/g.tanhaei/datar-web) was originally developed as an accompanying application to the main DatAR application. It allowed for the viewing of sentences containing co-occurrences between two brain concepts in a web interface, by using the **ExportConceptPair** widget (see [Export widgets](#export-widgets)). This application assumed that the DatAR application would be running on the same computer in order to locally send data through a RabbitMQ server. However, the Hololens 2 is a standalone platform, and thus does not support this type of web hosting.

In order to properly port over the web application for Hololens 2 support without requiring a separate hosting service, we must look into hosting the web-app on the Hololens 2 and allowing any device to connect to the web-app using the Hololens 2 IP-address. One way to accomplish this is by using the **[Device Portal](https://docs.microsoft.com/en-us/windows/uwp/debug-test-perf/device-portal-plugin)**. This is an officially supported mechanism for developing and running UWP apps on the Hololens 2. A beginning has been made researching this avenue (see the `/web/DatAR-Web` folder) but as it stands not much progress has been made. 

## Known issues

Some problems arose during the port from the original DatAR implementation, which was built using the SteamVR framework, to the Hololens 2 version using Microsoft Mixed Reality Toolkit. This is a compiled list of all known issues.
 - Widget Factories do not have an outline while hovering over them. This has purposefully been disabled as the generated widget created outline conflicts with the factory cube itself.
 - The accompanying DatAR web application has not been ported over to run from the Hololens 2, and has temporarily been replaced with an in-app viewer. See [DatAR Web Application - Hololens 2 Edition](#datar-web-application---hololens-2-edition).

