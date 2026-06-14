import pandas as pd
import joblib
from sklearn.model_selection import train_test_split
from sklearn.ensemble import RandomForestClassifier
from sklearn.metrics import accuracy_score, precision_score, recall_score, f1_score, confusion_matrix

def train():
    # Cargar datos
    df = pd.read_csv("app/ai/dataset_riesgo.csv")
    
    # Mapeo explícito solicitado de categorías
    category_map = {
        'phishing': 0,
        'password': 1,
        'wifi': 2,
        'social_engineering': 3
    }
    df['failed_category_encoded'] = df['failed_category'].map(category_map)
    
    # Selección de Features (X) y Target (y)
    feature_cols = [
        'total_points', 'correct_decisions', 'total_decisions', 'accuracy',
        'risk_score', 'awareness_score', 'decisions_last_7_days', 'failed_category_encoded'
    ]
    
    X = df[feature_cols]
    y = df['risk_level']
    
    # Split 80/20
    X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.2, random_state=42, stratify=y)
    
    # Clasificador estructurado según tus indicaciones
    model = RandomForestClassifier(
        n_estimators=100,
        max_depth=5,
        random_state=42
    )
    model.fit(X_train, y_train)
    
    # Predicciones de prueba
    y_pred = model.predict(X_test)
    
    # Métricas requeridas
    print("=== MÉTRICAS DEL MODELO (FASE 1) ===")
    print(f"Accuracy:  {accuracy_score(y_test, y_pred):.4f}")
    print(f"Precision: {precision_score(y_test, y_pred, average='weighted'):.4f}")
    print(f"Recall:    {recall_score(y_test, y_pred, average='weighted'):.4f}")
    print(f"F1 Score:  {f1_score(y_test, y_pred, average='weighted'):.4f}")
    print("\nMatriz de Confusión:")
    print(confusion_matrix(y_test, y_pred))
    
    # Guardar artefactos
    joblib.dump(model, "app/ai/model.pkl")
    joblib.dump(category_map, "app/ai/encoder.pkl")
    print("\nModelos guardados exitosamente ('model.pkl' y 'encoder.pkl').")

if __name__ == "__main__":
    train()