import React from "react";
import { View, Text, Button } from "react-native";
import { sendDecision } from "../services/api";

export default function GameScreen() {

  const handleChoice = async () => {
    const result = await sendDecision({
      user_id: 1,
      scenario_id: 1,
      choice: "click_link",
    });

    console.log(result);
  };

  return (
    <View>
      <Text>Simulación de Phishing</Text>
      <Button title="Abrir enlace sospechoso" onPress={handleChoice} />
    </View>
  );
}
