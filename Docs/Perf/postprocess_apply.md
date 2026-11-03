## Post Processing Apply Setup
- Profile: Assets/01.Scenes/SampleScene/FactoryPostProfile.asset
  - Color Grading: HDR, Neutral, Post Exposure 0.45, Contrast 8, Saturation -10, Temperature 4
  - Bloom: Intensity 1.2, Threshold 1.1, Soft Knee 0.5, Diffusion 6, Fast Mode
  - Vignette: Intensity 0.22, Smoothness 0.4
  - Ambient Occlusion: MSVO, Intensity 0.35
- Volume: Global Volume (Global, Layer PostProcessing)
- Main Camera: PostProcessLayer (Volume Layer PostProcessing, SMAA Medium)
- MinimapCamera: PostProcessLayer 없음 (미니맵에는 효과가 들지 않는다)
