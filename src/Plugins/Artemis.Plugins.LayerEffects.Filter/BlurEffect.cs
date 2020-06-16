﻿using Artemis.Core.Plugins.LayerEffect.Abstract;
using SkiaSharp;

namespace Artemis.Plugins.LayerEffects.Filter
{
    public class BlurEffect : LayerEffect<BlurEffectProperties>
    {
        public override void EnableLayerEffect()
        {
        }

        public override void DisableLayerEffect()
        {
        }

        public override void Update(double deltaTime)
        {
        }

        public override void PreProcess(SKCanvas canvas, SKImageInfo canvasInfo, SKPath path, SKPaint paint)
        {
            paint.ImageFilter = SKImageFilter.CreateBlur(
                Properties.BlurAmount.CurrentValue.Width,
                Properties.BlurAmount.CurrentValue.Height,
                paint.ImageFilter
            );
        }

        public override void PostProcess(SKCanvas canvas, SKImageInfo canvasInfo, SKPath path, SKPaint paint)
        {
        }

        private void UpdateFilterType()
        {
        }
    }
}