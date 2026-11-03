# 벤치마크 결과: 00_before

- 측정 시각: 2026-11-03 09:37:14
- Unity 6000.3.22f1 / NVIDIA GeForce RTX 4060 Laptop GPU / Intel(R) Core(TM) i9-14900HX
- 해상도 1920x1080 / Quality: PC / 에디터: True
- 시점당 워밍업 1.5s + 측정 4s, 랜덤 시드 12345

| 시점 | 평균 FPS | 평균 프레임(ms) | 1% Low FPS | CPU Main(ms) | CPU Render(ms) | GPU(ms) | Batches | SetPass | Draw Calls | Tris | Verts | Shadow Casters | GC Alloc/프레임(KB) | Total Used Mem(MB) | Texture Mem(MB) | Mesh Mem(MB) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| VP1_Street | 100.3 | 9.97 | 87.4 | 2.63 | 2.40 | 1.98 | 165 | 66 | 355 | 823540 | 576605 | 221 | 41.05 | 2155.2 | 725.7 | 38.3 |
| VP2_SpawnRoad | 99.2 | 10.08 | 73.1 | 2.66 | 2.51 | 2.05 | 227 | 74 | 336 | 831019 | 556387 | 232 | 28.11 | 2176.3 | 725.7 | 38.3 |
| VP3_Overview | 100.1 | 9.99 | 86.8 | 2.69 | 2.48 | 2.10 | 333 | 73 | 829 | 1310299 | 965960 | 58 | 41.90 | 2204.6 | 725.7 | 38.3 |
