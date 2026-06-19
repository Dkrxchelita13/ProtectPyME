# ProtectPyME
Repositorio ProtectPyME
# ProtectPyme

Sistema de capacitación en ciberseguridad para pequeñas y medianas empresas mediante gamificación, análisis de comportamiento e inteligencia artificial.

## Descripción

ProtectPyme es una plataforma educativa desarrollada para fortalecer la concientización en ciberseguridad dentro de las PyMEs. El sistema utiliza escenarios interactivos, minijuegos y métricas de desempeño para evaluar y mejorar las habilidades de los usuarios frente a amenazas comunes como phishing, contraseñas inseguras y dispositivos sospechosos.

El proyecto está compuesto por:

* Frontend desarrollado en Unity.
* Backend desarrollado con FastAPI.
* Base de datos PostgreSQL.
* Autenticación mediante JWT.
* Sistema de gamificación.
* Sistema de analíticas de usuario.
* Leaderboards y badges.
* Módulo de Inteligencia Artificial (en desarrollo).

---

## Características principales

### Autenticación

* Registro de usuarios.
* Inicio de sesión mediante JWT.
* Gestión de roles.

### Escenarios de Ciberseguridad

* Phishing Email.
* Contraseña comprometida.
* USB sospechoso.

Cada decisión tomada por el usuario es almacenada y evaluada para generar métricas de desempeño.

### Minijuegos

* Quiz de ciberseguridad.
* Crucigrama.
* Sopa de letras.

### Gamificación

* Sistema de puntos.
* Niveles.
* Badges.
* Ranking global.
* Ranking semanal.

### Analíticas

El sistema calcula automáticamente:

* Accuracy (% de respuestas correctas).
* Awareness Score.
* Risk Index.
* Categoría con más errores.
* Decisiones realizadas en los últimos 7 días.

### Inteligencia Artificial (Próximamente)

Integración de modelos Random Forest para:

* Predicción de riesgo.
* Detección de usuarios vulnerables.
* Recomendaciones personalizadas de capacitación.

---

## Arquitectura

```text
Unity
   │
   ▼
FastAPI REST API
   │
   ▼
PostgreSQL
   │
   ├── Users
   ├── Decisions
   ├── Scenarios
   ├── Badges
   ├── Leaderboard
   └── Analytics
```

---

## Tecnologías utilizadas

### Frontend

* Unity
* C#
* TextMeshPro

### Backend

* Python
* FastAPI
* SQLAlchemy
* JWT Authentication
* Pydantic

### Base de Datos

* PostgreSQL

### DevOps

* Docker
* Docker Compose
* Git
* GitHub

---

## Estructura del proyecto

```text
ProtectPyme
│
├── protectpyme-app
│   ├── backend
│   │   ├── app
│   │   │   ├── routes
│   │   │   ├── services
│   │   │   ├── crud
│   │   │   ├── models
│   │   │   ├── schemas
│   │   │   └── ai
│   │   │
│   │   └── main.py
│   │
│   └── unity
│       ├── Assets
│       ├── Scenes
│       ├── Scripts
│       └── Prefabs
│
└── README.md
```

---

## Instalación del Backend

### Clonar repositorio

```bash
git clone https://github.com/Dkrxchelita13/ProtectPyme.git
cd ProtectPyme
```

### Ejecutar con Docker

```bash
docker compose up --build
```

Backend disponible en:

```text
http://localhost:8000
```

Swagger:

```text
http://localhost:8000/docs
```

---

## Configuración de Unity

Configurar la URL del backend en:

```csharp
APIManager.cs
```

Ejemplo:

```csharp
private string baseUrl = "http://localhost:8000";
```

o

```csharp
private string baseUrl = "http://IP_DEL_SERVIDOR:8000";
```

---

## Endpoints principales

### Autenticación

```http
POST /login
POST /users
```

### Escenarios

```http
GET /scenarios
GET /scenarios/{id}
```

### Decisiones

```http
POST /decisions
GET /decisions/me
```

### Analytics

```http
GET /users/me/analytics
```

### Leaderboard

```http
GET /leaderboard
GET /leaderboard/me
GET /leaderboard/weekly
```

### Badges

```http
GET /users/me/badges
```

---

## Métricas utilizadas

### Awareness Score

Mide el nivel de concientización en ciberseguridad considerando:

* Accuracy.
* Total de puntos.
* Nivel de riesgo.

### Risk Index

Estima el nivel de vulnerabilidad del usuario considerando:

* Decisiones incorrectas.
* Historial de riesgo.
* Desempeño general.

---

## Autores

Proyecto desarrollado como parte de residencia profesional y tesina de Ingeniería en Sistemas Computacionales.

### Integrantes


* Equipo ProtectPyme

---

## Licencia

Proyecto académico desarrollado con fines educativos y de investigación.
