Disunity
========

Alternative implementation of a subset of Unity3D (LGPL/Commercial) to allow Unity3D-based code to migrate elsewhere



Components
==========
* https://github.com/kitsilanostealth/Disunity.UnityEngine - Alternative implementation of a subset of the runtime assembly
* https://github.com/kitsilanostealth/Disunity.UnityEditor - Alternative implementation of a subset of the editor assembly


Test cases
==========
* https://github.com/kitsilanostealth/Disunity.Bootcamp - from http://u3d.as/content/unity-technologies/bootcamp/28W
* https://github.com/kitsilanostealth/Disunity.RobotLab - from https://www.assetstore.unity3d.com/en/#!/content/7006


Phase I
=======
* Attempt to load the data for ANY Unity project, so we can get a grasp on the complexity of the data schema.

Phase II
========
* Attempt to build an alternative implementation of a key subset of the core runtime functionality.
* Maybe MonoBehaviour and Object, for example?
* How do Unity Coroutines map onto C# 5.0 async?
