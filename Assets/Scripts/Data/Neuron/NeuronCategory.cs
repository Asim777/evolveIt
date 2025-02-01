namespace Data.Neuron
{
    public interface INeuronCategory
    {
    }

    public abstract class SensorCategory : INeuronCategory
    {
        // Private constructor prevents external instantiation
        private SensorCategory()
        {
        }
        
        // Singleton instances for each Sensor type
        public static readonly SensorCategory Obstacle = new ObstacleCategory();
        public static readonly SensorCategory Entity = new EntityCategory();
        public static readonly SensorCategory Food = new FoodCategory();
        public static readonly SensorCategory EntityDensity = new EntityDensityCategory();
        public static readonly SensorCategory GeneticSimilarity = new GeneticSimilarityCategory();

        // Sealed implementations
        private sealed class ObstacleCategory : SensorCategory
        {
        }

        private sealed class EntityCategory : SensorCategory
        {
        }
        
        private sealed class FoodCategory : SensorCategory
        {
        }
        
        private sealed class EntityDensityCategory : SensorCategory
        {
        }
        
        private sealed class GeneticSimilarityCategory : SensorCategory
        {
        }
    }

    public abstract class InnerCategory : INeuronCategory
    {
        // Private constructor prevents external instantiation
        private InnerCategory()
        {
        }
        
        // Singleton instances for each Inner type
        public static readonly InnerCategory Logical = new LogicalCategory();
        public static readonly InnerCategory Numerical = new NumericalCategory();
        
        // Sealed implementations
        private sealed class LogicalCategory : InnerCategory
        {
        }
        
        private sealed class NumericalCategory : InnerCategory
        {
        }
    }

    public abstract class SinkCategory : INeuronCategory
    {
        // Private constructor prevents external instantiation
        private SinkCategory()
        {
        }
        
        // Singleton instances for each Sink type
        public static readonly SinkCategory Movement = new MovementCategory();
        public static readonly SinkCategory Turn = new TurnCategory();
        public static readonly SinkCategory Eat = new EatCategory();
        public static readonly SinkCategory Mate = new MateCategory();
        
        // Sealed implementations
        private sealed class MovementCategory : SinkCategory
        {
        }
        
        private sealed class TurnCategory : SinkCategory
        {
        }
        
        private sealed class EatCategory : SinkCategory
        {
        }
        
        private sealed class MateCategory : SinkCategory
        {
        }
    }
}