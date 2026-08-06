using AsyncProcessingApp.Models;

namespace AsyncProcessingApp;

public static class EventProcessor
{
    public static EnrichedMainTopicEvent CreateEnrichedEvent(MainTopicEvent payload, DateTimeOffset eventEnqueuedAt)
    {
        long complexityResult = ComputeComplexity(payload.Content);

        return new EnrichedMainTopicEvent
        {
            Id = payload.Id,
            Content = payload.Content,
            CreatedAt = payload.CreatedAt,
            SentAt = payload.SentAt,
            AreaCode = payload.AreaCode,
            EventEnqueuedAt = eventEnqueuedAt,
            ComputationResult = complexityResult,
            ProcessedAt = DateTimeOffset.UtcNow
        };
    }

    /*
     * CPU-intensive workload with complexity proportional to `content` (clamped 1–500).
     * Finds the first (content * 20) primes and accumulates a rolling checksum via
     * modular hashing, higher content values require significantly more prime-sieving work.
     */
    private static long ComputeComplexity(int content)
    {
        var boundedContent = Math.Clamp(content, 1, 500);
        long primeTarget = boundedContent * 20;

        long checksum = 0;
        long foundPrimes = 0;
        long candidate = 2;

        while (foundPrimes < primeTarget)
        {
            if (IsPrime(candidate))
            {
                checksum = (checksum * 31 + candidate) % 1_000_000_007;
                foundPrimes++;
            }

            candidate++;
        }

        return checksum;
    }

    private static bool IsPrime(long number)
    {
        switch (number)
        {
            case <= 1:
                return false;
            case 2:
                return true;
        }

        if (number % 2 == 0)
        {
            return false;
        }

        var limit = (long)Math.Sqrt(number);
        for (long divisor = 3; divisor <= limit; divisor += 2)
        {
            if (number % divisor == 0)
            {
                return false;
            }
        }

        return true;
    }
}
