# uSpringBone



## Overview

It is a SpringBone which performed speed up using JobSystem.
You can suppress the load of MainThread and let the calculation process be executed by WorkerThread.

Since have only implemented basic functions yet, plan to implement various additional implementations in the future.




## How to use

Please see the sample scenes included in the project for specific usage.

The basic usage is as follows.

* Attach the SpringBone component to the object.
* Attach a SpringBoneChain to an object that will be the parent of all Attached SpringBones.
* Attach SpringBoneCollider to arbitrary object as necessary and register it in SpringBoneChain.



## About the performance


uSpringBone implements such that complex rotation calculation is separated from MainThread and WorkerThread is used efficiently.


## Future renovation

* Addition of smooth rotation function
* Addition of rotation area restriction function
* Additional shape of Collision
* Implementation of EditorWindow to make setup easy
