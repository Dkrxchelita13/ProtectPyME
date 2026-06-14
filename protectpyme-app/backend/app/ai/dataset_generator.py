import pandas as pd
import numpy as np
import os

def generate_synthetic_data(output_path="app/ai/dataset_riesgo.csv"):
    np.random.seed(42)
    n_samples = 550 # Generamos un poco más para asegurar consistencia tras filtros
    
    # 1. Generar variables base de forma aleatoria estructurada
    total_decisions = np.random.randint(10, 100, size=n_samples)
    correct_decisions = np.array([np.random.randint(0, td + 1) for td in total_decisions])
    accuracy = (correct_decisions / total_decisions) * 100
    
    total_points = correct_decisions * 10 + np.random.randint(0, 50, size=n_samples)
    decisions_last_7_days = np.array([np.random.randint(0, min(td, 20) + 1) for td in total_decisions])
    
    # Scores de riesgo y concientización cruzados numéricamente
    awareness_score = accuracy * 0.8 + np.random.uniform(0, 20, size=n_samples)
    awareness_score = np.clip(awareness_score, 0, 100)
    
    risk_score = 100 - awareness_score + np.random.uniform(-10, 10, size=n_samples)
    risk_score = np.clip(risk_score, 0, 100)
    
    categories = ['phishing', 'password', 'wifi', 'social_engineering']
    failed_category = np.random.choice(categories, size=n_samples)
    
    # 2. Lógica de negocio para determinar el risk_level (0=Bajo, 1=Medio, 2=Alto)
    risk_level = []
    for i in range(n_samples):
        # Reglas basadas en tus criterios de coherencia
        if risk_score[i] < 35 and awareness_score[i] > 65 and accuracy[i] > 70:
            level = 0 # Bajo
        elif risk_score[i] > 65 or awareness_score[i] < 40 or accuracy[i] < 45:
            level = 2 # Alto
        else:
            level = 1 # Medio
        risk_level.append(level)
        
    df = pd.DataFrame({
        'total_points': total_points,
        'correct_decisions': correct_decisions,
        'total_decisions': total_decisions,
        'accuracy': accuracy,
        'risk_score': risk_score,
        'awareness_score': awareness_score,
        'decisions_last_7_days': decisions_last_7_days,
        'failed_category': failed_category,
        'risk_level': risk_level
    })
    
    # Recortar exactamente a 500 filas de manera balanceada
    df = df.head(500)
    
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
    df.to_csv(output_path, index=False)
    print(f"Dataset sintético generado con éxito en: {output_path} ({len(df)} registros)")

if __name__ == "__main__":
    generate_synthetic_data()