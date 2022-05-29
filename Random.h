#include <limits>

class Random
{
private:
    const int MBIG = INT_MAX;
    const int MSEED = 161803398;
    const int MZ = 0;

    int inext;
    int inextp;
    int *SeedArray = new int[56]();

    double Sample();
    double GetSampleForLargeRange();
    int InternalSample();

public:
    Random(int seed);
    ~Random();
    int Next();
    int Next(int minValue, int maxValue);
    int Next(int maxValue);
    double NextDouble();
};