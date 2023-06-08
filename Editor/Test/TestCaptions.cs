using NUnit.Framework;
using UnityEngine;

public class TestCaptions
{
    private Captions captionsObject;

    [SetUp]
    public void Setup()
    {
        captionsObject = ScriptableObject.CreateInstance<Captions>();

        // Define dos intervalos de tiempo
        var interval1 = new CaptionInterval
        {
            EntryTime = new CaptionIntervalKeyframe { Hours = 0, Minutes = 2, Seconds = 0, Milliseconds = 0 },
            ExitTime = new CaptionIntervalKeyframe { Hours = 0, Minutes = 4, Seconds = 0, Milliseconds = 0 }
        };

        var interval2 = new CaptionInterval
        {
            EntryTime = new CaptionIntervalKeyframe { Hours = 1, Minutes = 30, Seconds = 0, Milliseconds = 500 },
            ExitTime = new CaptionIntervalKeyframe { Hours = 1, Minutes = 31, Seconds = 0, Milliseconds = 500 }
        };

        var interval3 = new CaptionInterval
        {
            EntryTime = new CaptionIntervalKeyframe { Hours = 0, Minutes = 0, Seconds = 1, Milliseconds = 500 },
            ExitTime = new CaptionIntervalKeyframe { Hours = 0, Minutes = 0, Seconds = 1, Milliseconds = 0 }
        };

        //captionsObject.TimeIntervals = new CaptionIntervalKeyframe[] { interval1, interval2, interval3 };
        captionsObject.TimeIntervals.Add(interval1);
        captionsObject.TimeIntervals.Add(interval2);
        captionsObject.TimeIntervals.Add(interval3);
    }

    [Test]
    public void TestGetVisualizationTime()
    {
        // Comprobar que GetVisualizationTime devuelve el valor esperado para el primer intervalo
        Assert.AreEqual(120f, captionsObject.GetVisualizationTime(0));

        // Comprobar que GetVisualizationTime devuelve el valor esperado para el segundo intervalo
        Assert.AreEqual(60f, captionsObject.GetVisualizationTime(1));

        // Comprobar que GetVisualizationTime devuelve -1 para un índice fuera de rango
        Assert.AreEqual(-1f, captionsObject.GetVisualizationTime(99));

        // Comprobar que GetVisualizationTime devuelve -2 para entry time mayor que exit time
        Assert.AreEqual(-2f, captionsObject.GetVisualizationTime(2));
    }
}
