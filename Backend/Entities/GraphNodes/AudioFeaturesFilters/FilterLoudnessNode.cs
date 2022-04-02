﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Backend.Entities.GraphNodes.AudioFeaturesFilters
{
    public class FilterLoudnessNode : FilterRangeNode
    {
        // if db is pre-AudioFeatures even including AudioFeatures results in AudioFeature being null
        protected override int? GetValue(Track t) => (int)t.AudioFeatures?.Loudness;
        public override bool RequiresAudioFeatures => true;
    }
}
