
# gpu-instancing-demo

A Unity rendering prototype exploring the performance and architectural differences between traditional GameObject-based object rendering and GPU instancing.

The project generates vegetation procedurally on a terrain and allows the rendering strategy, object count, shadows, and wind parameters to be changed interactively.


**Note:** The GPU instancing solution was stripped from production and recontextualized for this repo. Be aware that specific dependencies, functionalities and optimizations have been removed or simplified.


## Features

- Procedural placement of vegetation within a configurable radius
- Terrain height sampling for natural placement
- Multiple plant prefabs
- CPU rendering using standard Unity GameObject instantiation
- GPU rendering using Graphics.DrawMeshInstanced
- Batching according to Unity's 1023-instance limit
- Per-instance wind offsets
- Shared wind parameters
- Optional shadow casting
- Runtime switching between CPU and GPU rendering
- Configurable instance count
- Reusable rendering buffers to avoid unnecessary per-frame allocations
- Precomputed transforms for static instances
- Per-prefab instance grouping to avoid repeatedly scanning the complete instance set




## Rendering Approaches

### CPU Mode

In CPU mode, each plant is instantiated as a regular Unity GameObject:
```
Instantiate(prefab, position, Quaternion.identity, transform);
```
This provides a useful baseline because each object participates in Unity's normal GameObject/Renderer workflow.

The approach is straightforward, but the CPU-side cost increases as the number of objects grows.

### GPU Instancing

GPU mode renders plants using:
```
Graphics.DrawMeshInstanced(...)
```
Instances are grouped by prefab and submitted in batches of up to 1023 instances.

The transforms are generated once and reused during rendering rather than recalculated every frame.

## Instance Data
The project separates instance generation from rendering.
Instance data is generated once and reused by both rendering modes.
Each instance stores:
- Position
- Scale
- Wind offset
- Prefab index
- Precomputed transformation matrix

This makes switching between CPU and GPU rendering possible without regenerating the procedural distribution (with the exception of toggling shadows).

It also means that static instance transforms do not need to be recalculated every frame.

## Wind
Wind is implemented via a vertex shader made in Shader Graph.

Wind parameters are shared across all vegetation:

- ```WindStrength``` - amplitude,
- ```WindSpeed``` - frequency,
- ```WindEnabled``` - set ```WindStrength``` and ```WindSpeed``` to 0, if false. 

Despite this, each instance receives its own offset via a per-instance property ```Offset``` in the shader, allowing the vegetation to avoid moving in perfect synchronization.

## Benchmark results

Work in progress! :)



## Limitations

### Scalability

For very large instance counts, approaches such as indirect/GPU-buffer-based rendering, GPU culling, or ECS may be worth investigating. ```Graphics.RenderMeshInstanced``` can also be considered depending on the Unity version and requirements, although it does not by itself eliminate CPU-side instance generation.

### Context dependency

The solution has limited modularity, as new per-instance shader properties (plant colours, wind influence weights, custom behaviours) have to be declared in code. The same goes for the prefabs used, as the solution assumes one submesh for each prefab and no nested mesh renderers in the prefab. These limitations are straightforward to address, but doing so would add complexity that felt unnecessary for the scope of this demo.

### 


## Authors

- Rafał Piechnik [@ravpiechnik](https://www.github.com/ravpiechnik)

External assets used:
- [Lowpoly Piece of Nature](https://assetstore.unity.com/packages/3d/environments/fantasy/lowpoly-piece-of-nature-40538) by [Evgenia Yaremko](https://assetstore.unity.com/publishers/9175); grass texture

- [Glowing Plants](https://sketchfab.com/3d-models/glowing-plants-0b80514d84c04589b5897e86e4764080) by [bluewombat](https://sketchfab.com/bluewombat); glowing plants
