using Microsoft.Extensions.Logging;

namespace SocuciusErgallaBotv3.Services
{
    public class QuoteService
    {
        private readonly ILogger<QuoteService> _logger;
        private Random _random = new Random();
        private string[] _quotes = new[]
        {
            @"Ahh yes, we've been expecting you. You'll have to be recorded before you're officially released. There are a few ways we can do this, and the choice is yours.",
            @"Very good. The letter that preceded you mentioned you were born under a certain sign. And what would that be?",
            @"Interesting. Now before I stamp these papers, make sure this information is correct.",
            @"Show your papers to the Captain when you exit to get your release fee.",
            @"Take your papers off the table and go see Captain Gravius.",
            @"The Imperial legion provides security for the Census and Excise Offices here in Seyda Neen. The troopers you see here are assigned to guard duty here. And the troopers outside, in Seyda Neen, they're guards -- officers of the law.",
            @"Yes. This is Morrowind. You're in the Census and Excise Offices in the port of Seyda Neen, in Vvardenfell District of the province of Morrowind.",
            @"Murdered you say? He has been missing for a while. I'll need proof of that before I begin an official inquiry, though",
            @"Murdered? What a waste. Processus was a good man. I had been wondering why we hadn't heard from him in a few days. Still, these are dangerous times we live in, and these sorts of things will happen. did you happen to find the tax money he'd collected?",
            @"Hope you'll bring that murderer to justice soon.",
            @"Quite a shame. He was a good man, and a loyal servant to the Emperor.",
            @"You've already spent it? That's not good to hear. That was Imperial tax money, after all. You'd do well to get that money back and return it to me.",
            @"The discovery of a dead body leads to the uncovering of intrigue in Seyda Neen"
        };

        public QuoteService(ILogger<QuoteService> logger)
        {
            _logger = logger;
        }

        public string GetRandomQuote()
        {
            int index = _random.Next(_quotes.Length);
            _logger.LogDebug($"Getting quote at index {index}");
            return _quotes[index];
        }
    }
}
