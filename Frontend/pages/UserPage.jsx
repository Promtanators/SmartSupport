import { Input, Button, Typography, Card, Avatar } from "antd";
import { SendOutlined, MessageOutlined } from "@ant-design/icons";
import { useState, useCallback } from "react";
import Particles from "react-tsparticles";
import { loadSlim } from "tsparticles-slim";
import particlesConfig from "./particles-config"; // Конфигурацию вынесем в отдельный файл

export default function UserPage({ handleClick }) {
  const [message, setMessage] = useState("");

  const particlesInit = useCallback(async (engine) => {
    await loadSlim(engine);
  }, []);

  return (
    <>
      <Particles
        id="tsparticles"
        init={particlesInit}
        options={particlesConfig} // Используем нашу конфигурацию
        style={{
          position: "absolute",
          top: 0,
          left: 0,
          width: "100%",
          height: "100%",
          zIndex: 0,
        }}
      />
      <div style={styles.container}>
        <Card style={styles.card}>
          <div style={styles.cardContent}>
            <Avatar
              size={64}
              icon={<MessageOutlined />}
              style={styles.avatar}
            />
            <Typography.Title level={2} style={{ marginTop: 16 }}>
              Нужна помощь?
            </Typography.Title>
            <Typography.Text type="secondary" style={styles.text}>
              Опишите вашу проблему или вопрос, и мы постараемся помочь как
              можно скорее.
            </Typography.Text>

            <div style={styles.inputContainer}>
              <Input.TextArea
                placeholder="Например: Не могу войти в личный кабинет"
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                autoSize={{ minRows: 2, maxRows: 6 }}
                style={{ flex: 1 }}
              />
              <Button
                type="primary"
                icon={<SendOutlined />}
                onClick={() => handleClick(message)}
                disabled={!message.trim()}
                style={{ height: "auto" }}
              />
            </div>
          </div>
        </Card>
      </div>
    </>
  );
}

const styles = {
  container: {
    display: "flex",
    justifyContent: "center",
    alignItems: "center",
    height: "100vh",
    position: "relative",
    zIndex: 1,
    padding: "20px",
  },
  card: {
    width: "100%",
    maxWidth: 600,
    borderRadius: "16px",
    boxShadow: "0 8px 24px rgba(0, 0, 0, 0.1)",
  },
  cardContent: {
    display: "flex",
    flexDirection: "column",
    alignItems: "center",
    textAlign: "center",
  },
  avatar: {
    backgroundColor: "#1677ff",
  },
  text: {
    marginBottom: "24px",
    fontSize: "16px",
  },
  inputContainer: {
    display: "flex",
    width: "100%",
    gap: "8px",
  },
};
