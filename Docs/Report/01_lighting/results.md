# 벤치마크 결과: 01_lighting

- 측정 시각: 2026-11-03 09:35:59
- Unity 6000.3.22f1 / NVIDIA GeForce RTX 4060 Laptop GPU / Intel(R) Core(TM) i9-14900HX
- 해상도 1920x1080 / Quality: PC / 에디터: True
- 시점당 워밍업 1.5s + 측정 4s, 랜덤 시드 12345

| 시점 | 평균 FPS | 평균 프레임(ms) | 1% Low FPS | CPU Main(ms) | CPU Render(ms) | GPU(ms) | Batches | SetPass | Draw Calls | Tris | Verts | Shadow Casters | GC Alloc/프레임(KB) | Total Used Mem(MB) | Texture Mem(MB) | Mesh Mem(MB) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| VP1_Street | 97.3 | 10.28 | 83.5 | 2.68 | 2.44 | 1.93 | 193 | 81 | 384 | 829526 | 588312 | 277 | 29.48 | 2171.1 | 729.7 | 38.3 |
| VP2_SpawnRoad | 95.7 | 10.45 | 79.6 | 2.73 | 2.57 | 1.91 | 285 | 87 | 408 | 857083 | 587781 | 357 | 44.80 | 2198.2 | 729.7 | 38.3 |
| VP3_Overview | 92.6 | 10.80 | 64.1 | 2.95 | 2.69 | 1.94 | 454 | 102 | 867 | 1318308 | 974257 | 86 | 45.09 | 2220.7 | 729.7 | 38.3 |
