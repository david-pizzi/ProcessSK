using Microsoft.SemanticKernel;
using ProcessSK.Data;

namespace ProcessSK.ProcessSteps;
// A process step to gather information about a product
public class GatherProductInfoStep : KernelProcessStep
{
    [KernelFunction]
    public async Task GatherProductInformationAsync(string productName, KernelProcessStepContext context)
    {
        Console.WriteLine($"[{nameof(GatherProductInfoStep)}]:\n\tGathering product information for product named {productName}");

        // Create a DocumentInfo object
        var productInfo = new DocumentInfo
        {
            Id = Guid.NewGuid().ToString(),
            Title = productName,
            Content = """
            Product Description:
            GlowBrew is a revolutionary AI driven coffee machine with industry leading number of LEDs and programmable light shows. The machine is also capable of brewing coffee and has a built in grinder.

            Product Features:
            1. **Luminous Brew Technology**: Customize your morning ambiance with programmable LED lights that sync with your brewing process.
            2. **AI Taste Assistant**: Learns your taste preferences over time and suggests new brew combinations to explore.
            3. **Gourmet Aroma Diffusion**: Built-in aroma diffusers enhance your coffee's scent profile, energizing your senses before the first sip.

            Troubleshooting:
            - **Issue**: LED Lights Malfunctioning
                - **Solution**: Reset the lighting settings via the app. Ensure the LED connections inside the GlowBrew are secure. Perform a factory reset if necessary.
            """
        };

        // Emit the event to pass productInfo to the next step
        await context.EmitEventAsync("ProductInfoGathered", productInfo);
    }
}
