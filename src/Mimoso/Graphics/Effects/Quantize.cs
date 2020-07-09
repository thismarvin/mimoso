﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Mimoso.Graphics.Effects
{
    class Quantize : FX
    {
        public int TotalColors { get; private set; }

        public Quantize(int totalColors) : base("Quantize")
        {
            TotalColors = totalColors;

            Initialize();
        }

        protected override void Initialize()
        {
            Effect.Parameters["TotalColors"].SetValue(TotalColors);
        }
    }
}
