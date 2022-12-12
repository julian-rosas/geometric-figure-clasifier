using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Src;

/// <summary>
///  Esta clase se encarga de clasificar una figura en específico asignandole un grupo de los 4 disponibles
/// </summary>
#pragma warning disable CA1416
class FigureClasificator
{
    /// <summary>
    /// Este método se encarga de llamar a los algoritmos auxiliares encargados de 
    /// asignar toda la logica detrás de clasificar una figura, al final se le asigna
    /// dicha clasificación como atributo de la figura en cuestión
    /// </summary>
    /// 
    /// <param name="figure"> la figura que se clasificará </param>
    public static void Clasificate(Figure figure)
    {
        Bitmap originalImage = figure.GetBitmap();
        int[] figureSignal = RayCasting(figure, 90, 450);

        int smoother = originalImage.Width < 120 ? 8 : 15;

        LinkedList<int> smoothSignal = SmoothSignal(figureSignal, smoother);

        int [] smoothSignalArr = smoothSignal.ToArray();
        Array.Sort(smoothSignalArr);

        if(IsCircle(smoothSignalArr)){
            figure.SetGroup(FigureGroups.Circles);
            return;
        }
        
        int numberOfPeaks = GetFigurePeaks(smoothSignal);
        figure.SetGroup(GetFigureGroup(numberOfPeaks));
    }

    /// <summary>
    /// Obtenemos el grupo al que pertenece la figura en función del número de 
    /// picos registrados.
    /// </summary>
    /// 
    /// <param name="peakNumber"> El número de picos registrados </param>
    /// <return> El grupo al que pertenece la figura </return>
    private static FigureGroups GetFigureGroup(int peakNumber)
    {
        if(peakNumber <= 3)
            return FigureGroups.Triangles;
        if(peakNumber == 4 || peakNumber == 5)
            return FigureGroups.Quadrilateral;
        return FigureGroups.Others;
    }

    /// <summary>
    /// Revisamos la señal recibida, si el valor máxima y mínima de ésta
    /// resulta ser menor a 8, entonces se trata de un círculo.
    /// </summary>
    /// 
    /// <param name="sortedSignal"> La señal ordenada </param>
    /// <return> Si la señal se atribuye a un círculo </return>
    private static bool IsCircle(int[] sortedSignal)
    {
        int max = sortedSignal[sortedSignal.Length-1];
        int min = sortedSignal[0];
        
        return (max-min) < 8;
    }

    /// <summary>
    /// Método privado estático encargado de evaluar la distancia que existe entre el centro de la figura
    /// hasta chocar con un borde de la misma, realiza este proceso radialmente hasta cubrir por completo
    /// toda la figura.
    /// </summary>
    ///
    /// <param name="figure"> la figura de la que se evaluará la distancia del centro a los bordes </param>
    /// <param name="gradIni"> El ángulo donde empezamos a lanzar rayos </param>
    /// <param name="gradFin"> El ángulo final donde paramos de lanzar rayos (la diferencia con gradIni debe ser 360) </param>
    /// <returns> figureSignal, un arreglo con la distancia registada de cada uno de los rayos en la figura</returns>
    public static int[] RayCasting(Figure figure, int gradIni, int gradFin)
    {
        int[] figureSignal = new int[360];

        int[] figureCenter = GetFigureCenter(figure);
        int xCenter = figureCenter[0];
        int yCenter = figureCenter[1];

        Color figureColor = figure.GetColor();
        Bitmap filteredFigure = figure.GetBitmap();

        int hypotenuse = 4;
        int index = 0;
        for(int degree = gradIni; degree < gradFin; degree += 1)
        {
            double radVersion = degree*(Math.PI/180);

            double unitX = Math.Cos(radVersion)*hypotenuse;
            double unitY = Math.Sin(radVersion)*hypotenuse;

            unitX = xCenter + unitX;
            unitY = yCenter + unitY;

            double dx = xCenter - unitX;
            double dy = yCenter - unitY;

            int stepsNumber = Math.Max(Math.Abs((int)dx), Math.Abs((int)dy));

            double xCoord = xCenter;
            double yCoord = yCenter;

            bool thresholdReached = false;
            int rayLength = 1;

            while (!thresholdReached)
            {
                yCoord += dy/stepsNumber;
                xCoord += dx/stepsNumber;

                if(!filteredFigure.GetPixel((int)Math.Ceiling(xCoord), (int)Math.Ceiling(yCoord)).Equals(figureColor))
                {
                    thresholdReached = true;
                    break;
                }
                rayLength++;
            }

            figureSignal[index] = rayLength;
            index++;
        }
        return figureSignal;
    }

    /// <summary>
    /// Método privado estático encargado de encontrar el centro de una figura
    /// </summary>
    /// 
    /// <param name="figure"> la figura que se clasificará </param>
    /// 
    /// <returns> centerCoords, un arreglo con las coordenadas del centro de la figura</returns>
    private static int[] GetFigureCenter(Figure figure)
    {

        Bitmap figureBitmap = (Bitmap)figure.GetBitmap().Clone();
        Color bgColor = figureBitmap.GetPixel(0,0);
        Color figureColor = figure.GetColor();
        int[] centerCoords = new int[2];
        int figurePixels = 0;

        centerCoords[0] = 0;
        centerCoords[1] = 0;;
        
        for(int x = 0; x < figureBitmap.Width; x++)
        {
            for(int y = 0; y < figureBitmap.Height; y++)
            {
                Color pixelColor = figureBitmap.GetPixel(x,y);

                if (pixelColor.Equals(figureColor))
                {
                    centerCoords[0] += x;
                    centerCoords[1] += y;
                    figurePixels++;
                }
            }
        }

        centerCoords[0] /= figurePixels;
        centerCoords[1] /= figurePixels;

       return centerCoords;
    }

    /// <summary>
    /// Obtenemos el número de picos máximos que encontremos 
    /// dentro de una señal suavizada.
    /// </summary>
    /// 
    /// <param name="smoothSignal"> La señal suavizada a trabajar</param>
    /// <return> numberPeaks, el número de picos máximos registrados </return>
    private static int GetFigurePeaks(LinkedList<int> smoothSignalList)
    {
        int[] smoothSignalArr = smoothSignalList.ToArray();
        int baseline = (int)smoothSignalArr.Average();
        int peakNum = 0;
        bool isAscending = false;

        for(int i = 1; i< smoothSignalArr.Length-1; i++)
        {
            int lastDelta = smoothSignalArr[i] - smoothSignalArr[i-1];
            int nextDelta = smoothSignalArr[i+1] - smoothSignalArr[i];
            if (lastDelta > 0)
                isAscending = true;
            if (lastDelta < 0)
                isAscending = false;
            if (smoothSignalArr[i] < baseline)
                continue;
            if ((lastDelta > 0 && nextDelta < 0) || (lastDelta == 0 && nextDelta < 0) && isAscending)
            {
                peakNum++;
                isAscending = false;
            }
        }
        if (isAscending)
            peakNum++;

        return peakNum;
    }

    /// <summary>
    /// Método auxiliar para calcular un promedio entre un intervalo de un arreglo
    /// </summary>
    /// 
    /// <param name="partialSignal"> El intervalo donde vamos a promediar </param>
    /// <param name="start"> el índice de inicio, inicio del subArreglo </param>
    /// <param name="finish"> el índice final, límite del subArreglo </param>
    /// <return> promedio del subarreglo </return>
    private static int GetAverage(int[] partialSignal, int start, int finish)
    {
        int sum = 0;

        int total = finish-start;

        for(int idx = start; idx <= finish; idx++)
        {
            sum += partialSignal[idx];
        }


        return sum/total;
    }

    /// <summary>
    /// Suaviza una señal promediando intervalos
    /// </summary>
    /// 
    /// <param name="signal"> Un arreglo, la señal recibida </param>
    /// <param name="range"> el rango de cuantos valores frente y detrás consideraremos para el promedio </param>
    /// <return> smoothSignal, la señal suavizada </return>
    private static LinkedList<int> SmoothSignal(int[] signal, int range)
    {
        LinkedList<int> smoothSignal = new LinkedList<int>();

        for(int idx = range ;idx < signal.Length-range; idx++)
        {
            int val = signal[idx];
            smoothSignal.AddLast(GetAverage(signal,idx-range,idx+range)); 
        }

        return smoothSignal;
    }
}
