[TOC]
# Model Importer

The LancerEdit model importer supports creating both .cmp and .3db files (corresponding to multi-part and single-part models) from common model interchange formats. It will also create .sur collision hitbox files when applicable. The model importer may be accessed by going to **Tools -> Import Model**.

![Menu Option](assets/modelimport-menu.png) 

## Supported Formats

### glTF 2.0

glTF 2.0 enjoys full support in the LancerEdit model importer, with the exception that the glTF 2.0 parser in LancerEdit may not access resources external to the selected file. Selecting a .glb export (or .gltf embedded in your modelling software) will ensure all resources are packed into the glTF file.

This includes textures and custom properties.

### Collada 

A fallback import/export format for when glTF 2.0 is not supported by your modelling tool. Custom properties are not available in this format, so hardpoints and compound joints will not be importer correctly.

### Wavefront .obj

Not recommended. Only supports creating single part .3db files, custom properties and multipart models are not supported by this format.

## Blender Integration

LancerEdit supports opening files from Blender, by performing an automatic export to glTF 2.0. LancerEdit will try to automatically detect an installation of Blender, but if it cannot find your installation of blender, you may set it manually in the options window.

- Go to **Tools -> Options** and set the Blender Path under the Blender tab.

## Model Hierarchy

LancerEdit requires your model to have the root mesh of your object at the root of your file, this will be the root part in the cmp, or the model of the 3db. It cannot be stored underneath an empty helper object. Children of that mesh are then stored as immediate children.

You can verify the generated model hierarchy in the **Output Nodes** tab of the model importer.

![Model Hierarchy](assets/modelimport-hierarchy.png)

### Hardpoints

Hardpoints are stored as children of your meshes with a series of custom properties attached to them. Hardpoints must have a property attached to them called `hardpoint` that is non-empty and non-zero, which indicates to the model importer that it is a hardpoint.

Fixed hardpoints have the custom property `hptype` with value `"fix"`.

Revolute hardpoints have their `hptype` property set to `"rev"`, and also contain the following properties:

| Name | Value |
|-|-|
| min | Minimum angle in degrees (e.g. -45) |
| max | Maximum angle in degrees (e.g. 45) |
| axis | Array of 3 floats describing the axis of rotation (e.g. [0, 1, 0]). <br><br> *Note: This property is always in Y-Up space. Blender and other modelling packages do not work in Y-Up coordinates* |

### Collision Hulls

Collision hulls are stored as direct children of their parent, and have names ending with the text `$hull`. These meshes **must** be convex and have an even number of faces, or they will not create a usable .sur file.

### LODs

LOD meshes can be stored anywhere in the file, and are named `part name` plus e.g. `$lod1` for the 1st lod. If your model has a part called `wing`, your lod meshes for that part will be called `wing$lod1` and `wing$lod2` etc.




