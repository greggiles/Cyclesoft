using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;

namespace ANTSniffer
{
    public class cDrawingEngine
    {
        public double chartZoom { get; set; }
        public cDrawingEngine()
        {
            chartZoom = 1.6;
        }

        public Point[] getWorkoutPoints(workoutDef activeWorkout)
        {

            Point[] tempWorkoutPoints = new Point[500];
            // not sure if I have to ...set all points?

            tempWorkoutPoints[0] = new Point(0.0, 1.0);

            // Can't do this here overUnders break this
            // tempWorkoutPoints[1 + 2 * activeWorkout.segments.Count] = new Point(1.0, 1.0);

            double currentX = 0.0;
            int currentPoint = 1;
            // would like to mod this to show
            // .2 FTP -> 1.8 FTP ... should make peeks better?
            // so, would like .2 =
            foreach (segmentDef seg in activeWorkout.segments)
            {
                tempWorkoutPoints[currentPoint] = new Point(currentX, (1 - ((seg.effort-1)/chartZoom+.5)));
                currentPoint++;
                switch (seg.type)
                {
                    case "steady":
                        currentX = currentX + (double)seg.length / activeWorkout.length;
                        tempWorkoutPoints[currentPoint] = new Point(currentX, (1 - ((seg.effort - 1) / chartZoom + .5)));
                        currentPoint++;
                        break;
                    case "ramp":
                        currentX = currentX + (double)seg.length / activeWorkout.length;
                        tempWorkoutPoints[currentPoint] = new Point(currentX, (1 - ((seg.effortFinish - 1) / chartZoom + .5)));
                        currentPoint++;
                        break;
                    case "overunder":
                        double endTime = currentX + (double)seg.length / activeWorkout.length;
                        double underTime = (double)seg.underTime / activeWorkout.length;
                        double overTime = (double)seg.overTime / activeWorkout.length;
                        bool isOver = false;
                        while (currentX < endTime)
                        {
                            if (!isOver && currentX + underTime <= endTime)
                            {
                                currentX = currentX + underTime;
                                tempWorkoutPoints[currentPoint] = new Point(currentX, (1 - ((seg.effort - 1) / chartZoom + .5)));
                                currentPoint++;
                                tempWorkoutPoints[currentPoint] = new Point(currentX, (1 - ((seg.effortFinish - 1) / chartZoom + .5)));
                                currentPoint++;
                                isOver = true;
                            }
                            else if (currentX + overTime <= endTime)
                            {
                                currentX = currentX + overTime;
                                tempWorkoutPoints[currentPoint] = new Point(currentX, (1 - ((seg.effortFinish - 1) / chartZoom + .5)));
                                currentPoint++;
                                tempWorkoutPoints[currentPoint] = new Point(currentX, (1 - ((seg.effort - 1) / chartZoom + .5)));
                                currentPoint++;
                                isOver = false;
                            }
                            else
                            {
                                currentX = endTime;
                                tempWorkoutPoints[currentPoint] = new Point(currentX, (1 - ((seg.effort - 1) / chartZoom + .5)));
                            }
                        }
                        break;
                }
            }
            tempWorkoutPoints[currentPoint] = new Point(1.0, 1.0);

            Point[] currentWorkoutPoints = new Point[currentPoint + 1];
            for (int i = 0; i <= currentPoint; i++)
                currentWorkoutPoints[i] = tempWorkoutPoints[i];

            return currentWorkoutPoints;
        }

        public List<Point> scaleLine(List<Point> inputPoints)
        {
            List<Point> returnPoints = new List<Point>();

            for (int i = 0; i < inputPoints.Count; i++)
            {
                Point rtnPt = new Point();
                rtnPt.X = inputPoints[i].X;
                rtnPt.Y = (2 * inputPoints[i].Y - 1) / chartZoom + .5;
                if (rtnPt.Y > 1) rtnPt.Y = 1;
                if (rtnPt.Y < 0) rtnPt.Y = 0;
                returnPoints.Add(rtnPt);
            }
            return returnPoints;
        }
    }
}
