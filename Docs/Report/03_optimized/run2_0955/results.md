# 벤치마크 결과: 03_optimized

- 측정 시각: 2026-11-03 09:55:01
- Unity 6000.3.22f1 / NVIDIA GeForce RTX 4060 Laptop GPU / Intel(R) Core(TM) i9-14900HX
- 해상도 1920x1080 / Quality: PC / 에디터: True
- 시점당 워밍업 1.5s + 측정 4s, 랜덤 시드 12345

| 시점 | 평균 FPS | 평균 프레임(ms) | 1% Low FPS | CPU Main(ms) | CPU Render(ms) | GPU(ms) | Batches | SetPass | Draw Calls | Tris | Verts | Shadow Casters | GC Alloc/프레임(KB) | Total Used Mem(MB) | Texture Mem(MB) | Mesh Mem(MB) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| VP1_Street | 72.7 | 13.76 | 25.2 | 3.43 | 5.22 | 4.43 | 157 | 73 | 244 | 422544 | 307687 | 112 | 53.13 | 1948.8 | 512.8 | 38.3 |
| VP2_SpawnRoad | 94.2 | 10.62 | 79.1 | 2.78 | 2.61 | 1.01 | 244 | 78 | 300 | 446753 | 314348 | 133 | 28.88 | 1970.8 | 513.9 | 38.3 |
| VP3_Overview | 95.5 | 10.47 | 82.7 | 2.85 | 2.49 | 1.00 | 443 | 96 | 813 | 927103 | 725316 | 4 | 28.70 | 1990.1 | 513.9 | 38.3 |
