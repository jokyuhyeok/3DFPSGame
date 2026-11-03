# 벤치마크 결과: 00_before

- 측정 시각: 2026-11-02 17:19:12
- Unity 6000.3.22f1 / NVIDIA GeForce RTX 4060 Laptop GPU / Intel(R) Core(TM) i9-14900HX
- 해상도 1920x1080 / Quality: PC / 에디터: True
- 시점당 워밍업 1.5s + 측정 4s, 랜덤 시드 12345

| 시점 | 평균 FPS | 평균 프레임(ms) | 1% Low FPS | CPU Main(ms) | CPU Render(ms) | GPU(ms) | Batches | SetPass | Draw Calls | Tris | Verts | Shadow Casters | GC Alloc/프레임(KB) | Total Used Mem(MB) | Texture Mem(MB) | Mesh Mem(MB) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| VP1_Street | 101.6 | 9.84 | 71.2 | 2.97 | 2.05 | 6.65 | 165 | 66 | 354 | 818315 | 573304 | 220 | 28.12 | 3736.4 | 733.9 | 38.6 |
| VP2_SpawnRoad | 102.9 | 9.72 | 67.1 | 2.88 | 2.03 | 6.87 | 239 | 75 | 349 | 833220 | 558474 | 235 | 41.93 | 4797.4 | 733.9 | 38.6 |
| VP3_Overview | 97.9 | 10.22 | 60.3 | 3.15 | 2.14 | 7.13 | 341 | 73 | 837 | 1312799 | 968868 | 58 | 42.64 | 5853.3 | 733.9 | 38.6 |
