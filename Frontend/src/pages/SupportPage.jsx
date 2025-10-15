import { useState, useEffect, useRef } from "react";
import { Card, Input, Button, Avatar, Typography, Row, Col, Modal } from "antd";
import {
  SendOutlined,
  UserOutlined,
  CustomerServiceOutlined,
  CommentOutlined,
} from "@ant-design/icons";

const cannedResponses = [
  { title: "Приветствие", text: "Добрый день! Чем могу помочь?" },
  {
    title: "Уточнение",
    text: "Для вашей безопасности, уточните, пожалуйста, последние 4 цифры номера вашей карты.",
  },
  {
    title: "Инструкция",
    text: "Выписку за прошлый месяц вы можете найти в мобильном приложении в разделе 'История операций' -> 'Заказать отчет'.",
  },
  {
    title: "Перевод",
    text: "К сожалению, этот вопрос вне моей компетенции. Перевожу ваш запрос на старшего специалиста.",
  },
  {
    title: "Благодарность",
    text: "Спасибо за ожидание! Я уточнил информацию по вашему вопросу.",
  },
  {
    title: "Завершение",
    text: "Рад был помочь! Если у вас появятся другие вопросы, обращайтесь. Всего доброго!",
  },
  {
    title: "Проблема",
    text: "Не могли бы вы, пожалуйста, описать проблему более подробно?",
  },
  {
    title: "Проверка",
    text: "Мне потребуется несколько минут, чтобы проверить данные. Пожалуйста, не отключайтесь.",
  },
];

export default function SupportOperatorPage({ message }) {
  const [supportInput, setSupportInput] = useState("");
  const [isActive, setIsActive] = useState(true);

  const [messages, setMessages] = useState(() =>
    message ? [{ id: Date.now(), text: message, sender: "user" }] : []
  );

  const [isModalOpen, setIsModalOpen] = useState(false);
  const handleModalKeyPress = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      if (userInput.trim()) {
        // Проверяем, что поле не пустое
        handleSendUserReply();
      }
    }
  };
  const [userInput, setUserInput] = useState("");
  // для прокрутки к сообщению при отправке
  const messagesEndRef = useRef(null);

  const addMessage = (text, sender) => {
    if (!text.trim()) return;
    setMessages((prev) => [...prev, { id: Date.now(), text, sender }]);
  };

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  const handleSend = () => {
    addMessage(supportInput, "operator");
    setSupportInput("");
  };

  const handleSendUserReply = () => {
    addMessage(userInput, "user");
    setIsModalOpen(false);
    setUserInput("Спасибо, вы мне очень помогли!");
  };

  const handleKeyPress = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  return (
    <div style={styles.pageWrapper}>
      <Row gutter={16} style={{ height: "100%" }}>
        <Col span={17} style={{ height: "100%" }}>
          <Card
            title="Диалог с клиентом"
            style={styles.chatCard}
            styles={{ body: styles.chatCardBody }}
          >
            <div style={styles.messageList}>
              {messages.map((msg) => (
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
                        style={styles.userAvatar}
                      />
                    )}
                    <Typography.Text
                      style={{
                        color: msg.sender === "operator" ? "#fff" : "inherit",
                      }}
                    >
                      {msg.text}
                    </Typography.Text>
                    {msg.sender === "operator" && (
                      <Avatar
                        size="small"
                        icon={<CustomerServiceOutlined />}
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
                  isActive ? "Введите ответ клиенту..." : "Диалог завершен."
                }
                value={supportInput}
                onChange={(e) => setSupportInput(e.target.value)}
                onKeyDown={handleKeyPress}
                autoSize={{ minRows: 1, maxRows: 5 }}
                disabled={!isActive}
                style={{ flex: 1 }}
              />
              <Button
                type="primary"
                icon={<SendOutlined />}
                onClick={handleSend}
                disabled={!supportInput.trim() || !isActive}
                style={{ marginRight: 8 }}
              >
                Ответить
              </Button>
              <Button
                icon={<CommentOutlined />}
                onClick={() => setIsModalOpen(true)}
                disabled={!isActive}
                style={{ marginRight: 8 }}
              >
                Клиент
              </Button>
              <Button
                danger
                onClick={() => setIsActive(false)}
                disabled={!isActive}
              >
                Завершить
              </Button>
            </div>
          </Card>
        </Col>

        <Col span={7} style={{ height: "100%" }}>
          <Card title="Шаблоны ответов" style={styles.templateCard}>
            {cannedResponses.map((res, index) => (
              <Card
                key={index}
                size="small"
                hoverable
                style={{
                  marginBottom: 12,
                  cursor: isActive ? "pointer" : "not-allowed",
                  opacity: isActive ? 1 : 0.5,
                }}
                onClick={() => isActive && setSupportInput(res.text)}
              >
                <Typography.Text strong>{res.title}</Typography.Text>
                <br />
                <Typography.Text type="secondary">{res.text}</Typography.Text>
              </Card>
            ))}
          </Card>
        </Col>
      </Row>

      <Modal
        title="Имитация запроса клиента"
        open={isModalOpen}
        onOk={handleSendUserReply}
        onCancel={() => setIsModalOpen(false)}
        okText="Отправить"
        cancelText="Отмена"
        centered
      >
        <Input.TextArea
          placeholder="Введите запрос от лица клиента."
          value={userInput}
          onChange={(e) => setUserInput(e.target.value)}
          autoSize={{ minRows: 3, maxRows: 5 }}
          style={{ marginTop: 16 }}
          onKeyDown={handleModalKeyPress}
        />
      </Modal>
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
    flex: 1,
    padding: "16px",
  },
  chatCardBody: {
    flex: 1,
    display: "flex",
    flexDirection: "column",
    padding: "16px",
  },
  messageList: { flex: 1, overflowY: "auto", marginBottom: "16px" },
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
    flex: 1,
    overflowY: "auto",
    padding: "16px",
  },
  messageContainer: { display: "flex", marginBottom: "12px" },
  userAvatar: { marginRight: 8, backgroundColor: "#87d068" },
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
