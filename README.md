# uSpringBone


[![GitHub license](https://img.shields.io/github/license/EsProgram/uSpringBone.svg)](https://github.com/EsProgram/uSpringBone/blob/master/LICENSE.txt)
[![release](https://img.shields.io/badge/release-wip-red.svg)](https://github.com/EsProgram/uSpringBone/releases)
[![PRs Welcome](https://img.shields.io/badge/PRs-welcome-blue.svg)](https://github.com/EsProgram/uSpringBone/pulls)


[![GitHub issues](https://img.shields.io/github/issues/EsProgram/uSpringBone.svg)](https://github.com/EsProgram/uSpringBone/issues)
[![GitHub forks](https://img.shields.io/github/forks/EsProgram/uSpringBone.svg)](https://github.com/EsProgram/uSpringBone/network)
[![GitHub stars](https://img.shields.io/github/stars/EsProgram/uSpringBone.svg)](https://github.com/EsProgram/uSpringBone/stargazers)
[![Twitter](https://img.shields.io/twitter/url/https/github.com/EsProgram/uSpringBone.svg?style=social)](https://twitter.com/intent/tweet?text=Wow:&url=https%3A%2F%2Fgithub.com%2FEsProgram%2FInkPainter)




## Overview

It is a SpringBone which performed speed up using ECS + JobSystem.

You can suppress the load of MainThread and let the calculation process be executed by WorkerThread.

Since have only implemented basic functions yet, plan to implement various additional implementations in the future.

<p align="center">
  <img src="https://github.com/EsProgram/uSpringBone/blob/master/Cap/dev01.gif" width="600"/>
</p>


## How to use

Please see the sample scenes included in the project for specific usage.

The basic usage is as follows.

* Attach the SpringBone component to the object.
* Attach a SpringBoneChain to an object that will be the parent of all Attached SpringBones.
* Attach SpringBoneCollider to arbitrary object as necessary and register it in SpringBoneChain.



## About the performance


uSpringBone implements such that complex rotation calculation is separated from MainThread and WorkerThread is used efficiently.

<p align="center">
  <img src="https://github.com/EsProgram/uSpringBone/blob/master/Cap/dev02.gif" width="600"/>
</p>

Even if multiple models that require a lot of calculation are arranged, the load on MainThread is minimized.


## Future renovation

* Addition of smooth rotation function
* Addition of rotation area restriction function
* Additional shape of Collision
* Implementation of EditorWindow to make setup easy



## Licence

* [BSD 3-Clause "New" or "Revised" License](https://github.com/EsProgram/uSpringBone/blob/master/LICENSE)
* © Unity Technologies Japan/UCL
