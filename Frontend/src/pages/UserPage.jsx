import { Input, Button, Typography, Card, Avatar } from "antd";
import { SendOutlined, MessageOutlined } from "@ant-design/icons";
import Particles from "react-tsparticles";
import particlesConfig from "./particles-config";
import { loadSlim } from "tsparticles-slim";
import { fetchData } from "../api";

export default function UserPage({
                                   onNextPage,
                                   message,
                                   setMessage,
                                   setData,
                                   setError,
                                 }) {
  const handleSubmit = async () => {
    if (!message.trim()) return;
    onNextPage();
    try {
      const result = await fetchData(message);
      setData(result);
      setError(null);
    } catch (error) {
      setError("Ошибка при получении подсказок");
      console.error(error);
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSubmit();
    }
  };

  return (
      <>
        <Particles
            id="tsparticles"
            init={loadSlim}
            options={particlesConfig}
            style={styles.particles}
        />
        <div style={styles.container}>
          <Card style={styles.card}>
            <Avatar size={64} icon={<MessageOutlined />} style={styles.avatar} />
            <Typography.Title level={2} style={{ marginTop: 16 }}>
              Нужна помощь?
            </Typography.Title>
            <div style={{ marginBottom: 24 }}>
              <Typography.Text type="secondary">
                Опишите вашу проблему или вопрос, и мы постараемся помочь.
              </Typography.Text>
            </div>
            <div style={styles.inputContainer}>
              <Input.TextArea
                  placeholder="Например: Не могу войти в личный кабинет"
                  value={message}
                  onChange={(e) => setMessage(e.target.value)}
                  onKeyDown={handleKeyPress}
                  autoSize={{ minRows: 2, maxRows: 6 }}
                  style={styles.textarea}
              />
              <Button
                  type="primary"
                  icon={<SendOutlined />}
                  onClick={handleSubmit}
                  disabled={!message.trim()}
                  style={styles.sendButton}
              />
            </div>
          </Card>
        </div>
      </>
  );
}

const styles = {
  particles: {
    position: "absolute",
    top: 0,
    left: 0,
    width: "100%",
    height: "100%",
    zIndex: 0,
  },
  container: {
    display: "flex",
    justifyContent: "center",
    alignItems: "center",
    height: "100vh",
    boxSizing: "border-box",
    padding: 20,
    position: "relative",
    zIndex: 1,
  },
  card: {
    width: "90%",
    maxWidth: 800,
    borderRadius: 20,
    boxShadow: "0 12px 32px rgba(0,0,0,0.15)",
    padding: 40,
    textAlign: "center",
    backgroundColor: "rgba(255,255,255,0.95)",
  },

  avatar: { backgroundColor: "#1677ff", marginBottom: 16 },
  text: {
    marginBottom: 24,
    fontSize: 16,
  },
  inputContainer: {
    display: "flex",
    gap: 8,
    alignItems: "stretch",
  },
  textarea: {
    flex: 1,
    resize: "none",
  },
  sendButton: {
    width: "auto",
    height: "auto",
    padding: "0 20px",
    display: "flex",
    alignItems: "center",
  },
};
