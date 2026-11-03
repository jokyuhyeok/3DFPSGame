# 벤치마크 결과: 03_optimized

- 측정 시각: 2026-11-03 09:52:51
- Unity 6000.3.22f1 / NVIDIA GeForce RTX 4060 Laptop GPU / Intel(R) Core(TM) i9-14900HX
- 해상도 1920x1080 / Quality: PC / 에디터: True
- 시점당 워밍업 1.5s + 측정 4s, 랜덤 시드 12345

| 시점 | 평균 FPS | 평균 프레임(ms) | 1% Low FPS | CPU Main(ms) | CPU Render(ms) | GPU(ms) | Batches | SetPass | Draw Calls | Tris | Verts | Shadow Casters | GC Alloc/프레임(KB) | Total Used Mem(MB) | Texture Mem(MB) | Mesh Mem(MB) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| VP1_Street | 92.6 | 10.80 | 55.6 | 2.89 | 1.81 | 1.32 | 152 | 70 | 235 | 419681 | 304801 | 112 | 44.26 | 2087.4 | 643.0 | 42.3 |
| VP2_SpawnRoad | 97.5 | 10.25 | 73.1 | 2.71 | 2.43 | 1.84 | 230 | 74 | 286 | 441482 | 307757 | 128 | 28.31 | 2107.2 | 643.0 | 42.3 |
| VP3_Overview | 98.7 | 10.13 | 80.2 | 2.71 | 2.42 | 1.87 | 439 | 94 | 808 | 924862 | 720531 | 4 | 42.25 | 2135.0 | 643.0 | 42.3 |
