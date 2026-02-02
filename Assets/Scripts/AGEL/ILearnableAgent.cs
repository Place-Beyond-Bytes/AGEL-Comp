using System.Collections.Generic;

namespace AGEL
{
    public interface ILearnableAgent 
    {
        LearningResult LearnFromFeedback(Feedback feedback);
        List<string> GetRulesLearned();
    }
    
    public class LearningResult
    {
        public int rulesAdded;
        public int rulesRetracted;
        public List<string> newRules = new List<string>();
    }
}