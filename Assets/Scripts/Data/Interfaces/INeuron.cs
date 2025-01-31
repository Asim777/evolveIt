using System.Collections.Generic;
using System.Linq;
using Data.Neuron;

namespace Data.Interfaces
{
    public interface INeuron(
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
            INeuron[] additionalNeurons = { };
            return activationGroup switch
            {
                ActivationGroup.AG13 =>
                  new INeuron[] 
                  {
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
                    },
                ActivationGroup.Ag18 => GetNeurons(ActivationGroup.AG13).Concat(
                        new INeuron[]
                        {
                            
                        }
                        )
                    .filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag18),
                ActivationGroup.Ag26 => GetAllNeurons()
                    .filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag26),
                ActivationGroup.Ag38 => GetAllNeurons()
                    .filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag38),
                ActivationGroup.Ag47 => GetAllNeurons()
                    .filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag47),
                ActivationGroup.Ag54 => GetAllNeurons()
                    .filter(neuron => neuron.ActivationGroup == ActivationGroup.Ag54),
                _ => throw new System.ArgumentException("Invalid number of Neurons")
            };
        }
    }
}