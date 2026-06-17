using TransactionFraudDetection.Domain;

namespace TransactionFraudDetection.Worker;

public class FraudExplanationProcessor(IFraudExplainer explainer, ExplanationFileWriter fileWriter)
{
    public async Task ProcessAsync(FraudCheckNotification notification)
    {
        if (!notification.Result.IsFraudulent)
        {
            return;
        }

        var prompt = FraudExplanationPromptBuilder.Build(notification);
        var explanation = await explainer.ExplainAsync(prompt);

        await fileWriter.WriteAsync(notification, explanation);
    }
}
