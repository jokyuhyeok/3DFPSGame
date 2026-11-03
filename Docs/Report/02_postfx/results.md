# 벤치마크 결과: 02_postfx

- 측정 시각: 2026-11-03 09:45:10
- Unity 6000.3.22f1 / NVIDIA GeForce RTX 4060 Laptop GPU / Intel(R) Core(TM) i9-14900HX
- 해상도 1920x1080 / Quality: PC / 에디터: True
- 시점당 워밍업 1.5s + 측정 4s, 랜덤 시드 12345

| 시점 | 평균 FPS | 평균 프레임(ms) | 1% Low FPS | CPU Main(ms) | CPU Render(ms) | GPU(ms) | Batches | SetPass | Draw Calls | Tris | Verts | Shadow Casters | GC Alloc/프레임(KB) | Total Used Mem(MB) | Texture Mem(MB) | Mesh Mem(MB) |
|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|---|
| VP1_Street | 96.0 | 10.42 | 80.4 | 2.81 | 2.57 | 1.98 | 193 | 81 | 383 | 829731 | 588501 | 278 | 27.66 | 2156.4 | 738.1 | 42.3 |
| VP2_SpawnRoad | 96.8 | 10.33 | 81.0 | 2.77 | 2.54 | 2.08 | 285 | 86 | 407 | 856372 | 586574 | 355 | 42.47 | 2180.6 | 738.1 | 42.3 |
| VP3_Overview | 91.8 | 10.89 | 75.7 | 3.12 | 2.76 | 1.99 | 454 | 102 | 867 | 1319128 | 974775 | 86 | 43.25 | 2202.6 | 738.1 | 42.3 |
