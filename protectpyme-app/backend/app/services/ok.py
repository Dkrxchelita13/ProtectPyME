def calcular_promedio(total, cantidad):
    """
    Calcula el promedio dividiendo el total entre la cantidad.
    Valida que los datos sean numéricos y que la cantidad no sea cero.
    """
    # Validación de tipo de datos (asegurar que sean int o float)
    if not isinstance(total, (int, float)) or not isinstance(cantidad, (int, float)):
        print("Error de tipo: Ambos argumentos deben ser números.")
        return None
        
    # Validación para evitar división por cero
    if cantidad == 0:
        print("Error matemático: No se puede dividir entre cero.")
        return None
        
    return total / cantidad

# --- Pruebas del script ---

# Prueba 1: Intento de división por cero (Antes Línea 4)
resultado = calcular_promedio(100, 0)

# Prueba 2: Impresión correcta de la variable existente (Antes Línea 6)
# Se corrige 'numeros_totales' por la variable 'resultado' que sí existe
print(f"Resultado de la Prueba 1: {resultado}")

# Prueba 3: Intento con tipo de dato string (Antes Línea 8)
resultado_tipo = calcular_promedio(100, "diez")
print(f"Resultado de la Prueba 3: {resultado_tipo}")

# Prueba 4: Flujo correcto (Caso de éxito)
resultado_exito = calcular_promedio(100, 10)
print(f"Resultado de un flujo correcto: {resultado_exito}")