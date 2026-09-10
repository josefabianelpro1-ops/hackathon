namespace SinaloaRiegoInteligente
{
    public class Parcela
    {
        public string Nombre { get; set; } = string.Empty;
        public string Cultivo { get; set; } = string.Empty;
        public double HumedadSuelo { get; set; } // Porcentaje actual (0 - 100%)
        public double Temperatura { get; set; }  // °C
        public double Hectareas { get; set; }    // Superficie del terreno
        public bool RiegoActivo { get; set; }

        // Texto descriptivo según el porcentaje de humedad
        public string EstadoRiego => HumedadSuelo switch
        {
            < 35 => "Peligro: Estrés Hídrico",
            >= 35 and <= 70 => "Estado Óptimo",
            _ => "Exceso de Agua / Sat."
        };

        // CÓDIGO DE COLOR DINÁMICO
        public string ColorEstado => HumedadSuelo switch
        {
            < 35 => "#EF4444",      // ROJO: Estrés Hídrico
            >= 35 and <= 70 => "#22C55E", // VERDE: Estado Óptimo
            _ => "#3B82F6"          // AZUL: Exceso / Saturación
        };

        /// <summary>
        /// Algoritmo de cálculo hídrico según cultivo y humedad actual.
        /// </summary>
        public double CalcularLitrosAguaNecesarios()
        {
            const double humedadObjetivo = 70.0;

            if (HumedadSuelo >= humedadObjetivo)
                return 0;

            double deficitPorcentaje = humedadObjetivo - HumedadSuelo;

            double factorCultivo = Cultivo.ToLower() switch
            {
                "maíz" => 1200,
                "jitomate" => 1500,
                "frijol" => 900,
                "garbanzo" => 850,
                _ => 1000
            };

            double factorTemperatura = Temperatura > 30.0 ? 1.15 : 1.0;
            double totalLitros = deficitPorcentaje * factorCultivo * Hectareas * factorTemperatura;

            return Math.Round(totalLitros, 0);
        }
    }
}