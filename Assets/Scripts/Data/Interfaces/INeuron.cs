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
                            new ObstacleFront(),
                            new EntityFront(),
                            new FoodFront(),
                            new And(),
                            new Or()
                        }
                    ),
                ActivationGroup.Ag26 => GetNeurons(ActivationGroup.Ag18).Concat(
                        new INeuron[]
                        {
                            new ObstacleLeft(),
                            new EntityLeft(),
                            new FoodLeft(),
                            new ObstacleRight(),
                            new EntityRight(),
                            new FoodRight(),
                            new Less05(),
                            new More05()
                        }
                    ),
                ActivationGroup.Ag38 => GetNeurons(ActivationGroup.Ag26).Concat(
                        new INeuron[]
                        {
                            new ObstacleBehind(),
                            new EntityBehind(),
                            new FoodBehind(),
                            new GeneticSimilarityBehind(),
                            new GeneticSimilarityAhead
                            new GeneticSimilarityAround(),
                            new GeneticSimilarityFront(),
                            new GeneticSimilarityLeft(),
                            new GeneticSimilarityRight(),
                            new Less01(),
                            new More09(),
                            new Not()
                        }
                    ),
                ActivationGroup.A47 => GetNeurons(ActivationGroup.Ag38).Concat(
                        new INeuron[]
                        {
                            new EntityDensityAhead
                            new EntityDensityAround(),
                            new EntityDensityFront(),
                            new EntityDensityLeft(),
                            new EntityDensityRight(),
                            new EntityDensityBehind(),
                            new Xor(),
                            new Less025(),
                            new More075()
                        }
                    ),
                ActivationGroup.A55 => GetNeurons(ActivationGroup.A47).Concat(
                        new INeuron[]
                        {
                            new Hunger(),
                            new ReproductiveDrive(),
                            new Energy(),
                            new Health(),
                            new Age(),
                            new P(),
                            new Less075(),
                            new More025()
                        }
                    ),
                _ => throw new System.ArgumentException("Invalid Activation Group")
            };
        }
    }
}
