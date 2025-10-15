import { useState, useEffect, useRef } from "react";
import { Card, Input, Button, Avatar, Typography, Row, Col } from "antd";
import { SendOutlined, UserOutlined, RobotOutlined } from "@ant-design/icons";

const { Text, Title } = Typography;

const cannedResponses = [
  {
    title: "Стандартное приветствие",
    text: "Добрый день! Меня зовут [Имя Оператора], чем могу помочь?",
  },
  {
    title: "Уточнение по карте",
    text: "Для вашей безопасности, уточните, пожалуйста, последние 4 цифры номера вашей карты.",
  },
  {
    title: "Инструкция по выписке",
    text: "Выписку за прошлый месяц вы можете найти в мобильном приложении в разделе 'История операций' -> 'Заказать отчет'.",
  },
  {
    title: "Перевод на специалиста",
    text: "К сожалению, этот вопрос вне моей компетенции. Перевожу ваш запрос на старшего специалиста. Пожалуйста, оставайтесь на линии.",
  },
  {
    title: "Благодарность за ожидание",
    text: "Спасибо за ожидание! Я уточнил информацию по вашему вопросу.",
  },
  {
    title: "Завершение диалога",
    text: "Рад был помочь! Если у вас появятся другие вопросы, обращайтесь. Всего доброго!",
  },
  {
    title: "Уточнение проблемы",
    text: "Не могли бы вы, пожалуйста, описать проблему более подробно?",
  },
  {
    title: "Проверка данных",
    text: "Мне потребуется несколько минут, чтобы проверить данные. Пожалуйста, не отключайтесь.",
  },
];

export default function SupportOperatorPage() {
  const [message, setMessage] = useState("");
  const [isChatActive, setIsChatActive] = useState(true);
  const [chatMessages, setChatMessages] = useState([
    {
      id: 1,
      text: "Здравствуйте, не могу найти выписку по карте за прошлый месяц. Помогите, пожалуйста.",
      sender: "user",
    },
  ]);

  const messagesEndRef = useRef(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  useEffect(scrollToBottom, [chatMessages]);

  const handleTemplateClick = (text) => {
    if (isChatActive) {
      setMessage(text);
    }
  };

  const handleSend = () => {
    if (message.trim() === "" || !isChatActive) return;
    const operatorMessage = {
      id: Date.now(),
      text: message,
      sender: "operator",
    };
    setChatMessages((prev) => [...prev, operatorMessage]);
    setMessage("");

    setTimeout(() => {
      const userReply = {
        id: Date.now() + 1,
        text: "Спасибо, вы мне очень помогли!",
        sender: "user",
      };
      setChatMessages((prev) => [...prev, userReply]);
      setIsChatActive(false);
    }, 1500);
  };

  return (
    <div style={styles.pageWrapper}>
      <Row gutter={16} style={{ height: "100%" }}>
        <Col span={17} style={{ height: "100%" }}>
          <Card
            title="Диалог с клиентом"
            style={styles.chatCard}
            bodyStyle={styles.chatCardBody}
          >
            <div style={styles.messageList}>
              {chatMessages.map((msg) => (
                <div
                  key={msg.id}
                  style={{
                    ...styles.messageContainer,
                    justifyContent:
                      msg.sender === "operator" ? "flex-end" : "flex-start",
                  }}
                >
                  <div style={styles.messageBubble(msg.sender)}>
                    {msg.sender === "user" && (
                      <Avatar
                        size="small"
                        icon={<UserOutlined />}
                        style={{ marginRight: 8, backgroundColor: "#87d068" }}
                      />
                    )}
                    <Text
                      style={{
                        color: msg.sender === "operator" ? "#fff" : "inherit",
                      }}
                    >
                      {msg.text}
                    </Text>
                    {msg.sender === "operator" && (
                      <Avatar
                        size="small"
                        icon={<RobotOutlined />}
                        style={{ marginLeft: 8 }}
                      />
                    )}
                  </div>
                </div>
              ))}
              <div ref={messagesEndRef} />
            </div>
            <div style={styles.inputArea}>
              <Input.TextArea
                placeholder={
                  isChatActive ? "Введите ответ клиенту..." : "Диалог завершен."
                }
                value={message}
                onChange={(e) => setMessage(e.target.value)}
                autoSize={{ minRows: 1, maxRows: 5 }}
                disabled={!isChatActive}
                style={{ flex: 1 }}
              />
              <Button
                type="primary"
                icon={<SendOutlined />}
                onClick={handleSend}
                disabled={!message.trim() || !isChatActive}
              />
            </div>
          </Card>
        </Col>

        <Col span={7} style={{ height: "100%" }}>
          <Card
            title="Шаблоны ответов"
            style={styles.templateCard}
            bodyStyle={styles.templateCardBody}
          >
            {cannedResponses.map((response, index) => (
              <Card
                key={index}
                size="small"
                hoverable
                style={{
                  marginBottom: 12,
                  cursor: isChatActive ? "pointer" : "not-allowed",
                }}
                onClick={() => handleTemplateClick(response.text)}
                bodyStyle={{ opacity: isChatActive ? 1 : 0.5 }}
              >
                <Text strong>{response.title}</Text>
                <br />
                <Text type="secondary">{response.text}</Text>
              </Card>
            ))}
          </Card>
        </Col>
      </Row>
    </div>
  );
}

const styles = {
  pageWrapper: {
    height: "100vh",
    background: "#f0f2f5",
    padding: "16px",
    boxSizing: "border-box",
  },
  chatCard: {
    height: "100%",
    display: "flex",
    flexDirection: "column",
  },
  chatCardBody: {
    flex: 1,
    display: "flex",
    flexDirection: "column",
    overflow: "hidden",
    padding: "16px",
  },
  messageList: {
    flex: 1,
    overflowY: "auto",
    marginBottom: "16px",
  },
  inputArea: {
    display: "flex",
    gap: "8px",
    borderTop: "1px solid #f0f0f0",
    paddingTop: "16px",
  },
  templateCard: {
    height: "100%",
    display: "flex",
    flexDirection: "column",
  },
  templateCardBody: {
    flex: 1,
    overflowY: "auto",
    padding: "16px",
  },
  messageContainer: {
    display: "flex",
    marginBottom: "12px",
  },
  messageBubble: (sender) => ({
    padding: "8px 12px",
    borderRadius: "16px",
    maxWidth: "80%",
    display: "flex",
    alignItems: "center",
    backgroundColor: sender === "operator" ? "#1890ff" : "#f0f0f0",
    color: sender === "operator" ? "#fff" : "#000",
  }),
};
