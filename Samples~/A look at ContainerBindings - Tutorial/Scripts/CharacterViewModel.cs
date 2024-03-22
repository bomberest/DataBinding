﻿using UnityEngine;

namespace JH.DataBinding.Examples.ContainerBindings.Tutorial
{
    public class CharacterViewModel : DataSourceBase<CharacterViewModel>
    {
        public string Name { get; set; }
        public string Class { get; set; }

        public int Level { get; set; }
        public string LevelString => $"{Level}";
        public float LevelProgress { get; internal set; }

        public AttributeViewModel[] Attributes { get; set; }
        public Sprite CharacterImage { get; set; }
    }
}
