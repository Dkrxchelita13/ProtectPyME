import { useRouter } from "expo-router";
import { StyleSheet, Text, TouchableOpacity, View } from "react-native";

export default function Home() {
  const router = useRouter();

  return (
    <View style={styles.container}>
      <Text style={styles.title}>ProtectPYME</Text>
      <Text style={styles.subtitle}>Menú Principal</Text>

      <TouchableOpacity style={styles.button}>
        <Text style={styles.buttonText}>🎮 Iniciar juego</Text>
      </TouchableOpacity>

      <TouchableOpacity style={styles.button}>
        <Text style={styles.buttonText}>🏆 Leaderboard</Text>
      </TouchableOpacity>

      <TouchableOpacity style={styles.button}>
        <Text style={styles.buttonText}>👤 Perfil</Text>
      </TouchableOpacity>

      <TouchableOpacity
        style={[styles.button, { backgroundColor: "#E74C3C" }]}
        onPress={() => router.replace("/login")}
      >
        <Text style={styles.buttonText}>Cerrar sesión</Text>
      </TouchableOpacity>
    </View>
  );
}

const styles = StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: "#0B1E3B",
    justifyContent: "center",
    padding: 20,
  },
  title: {
    fontSize: 30,
    color: "white",
    textAlign: "center",
    fontWeight: "bold",
  },
  subtitle: {
    color: "#9DB4FF",
    textAlign: "center",
    marginBottom: 30,
  },
  button: {
    backgroundColor: "#5B8DEF",
    padding: 15,
    borderRadius: 12,
    marginVertical: 8,
  },
  buttonText: {
    color: "white",
    textAlign: "center",
    fontWeight: "bold",
    fontSize: 16,
  },
});
