using System.Collections.Generic;
using System.Linq;

namespace Data.Interfaces
{
    public interface INeuron { }
    /*public interface INeuron(
        string id,
        INeuronCategory Category
    )
    {
    }

    // Extension method to get all neurons provided the ActivationGroup. We have 6 combinations of Neurons represented
    // by ActivationGroups each of which contains all Neurons from previous categories plus some new ones.
    public static class NeuronExtensions
    {
        public static INeuron[] GetNeurons(ActivationGroup activationGroup)
        {
            return activationGroup switch
            {
                ActivationGroup.Ag13 => GetAllNeurons()
                    .filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag13),
                ActivationGroup.Ag18 => GetNeurons(ActivationGroup.Ag13).Concat(GetAllNeurons())
                    .filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag18),
                ActivationGroup.Ag26 => GetAllNeurons().filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag26),
                ActivationGroup.Ag38 => GetAllNeurons().filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag38),
                ActivationGroup.Ag47 => GetAllNeurons().filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag47),
                ActivationGroup.Ag54 => GetAllNeurons().filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag54),
                _ => throw new System.ArgumentException("Invalid number of Neurons")
            };
        }
        
        // Returns array of all Neurons
        private static INeuron[] GetAllNeurons()
        {
            return new INeuron[]
            {
                // 13
                new ObstacleAhead(),
                new ObstacleAround(),
                new EntityAhead(),
                new EntityAround(),
                new FoodAhead(),
                new FoodAround(),
                new ObstacleEncountered(),
                new EntityEncountered(),
                new FoodEncountered(),
                new Move(),
                new Turn(),
                new Eat(),
                new Mate()
            };
        }
    }*/
}