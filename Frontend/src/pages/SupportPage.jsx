import React, { useState, useEffect, useRef } from "react";
import {
  Card,
  Input,
  Button,
  Avatar,
  Typography,
  Row,
  Col,
  Modal,
  Divider,
} from "antd";
import {
  SendOutlined,
  UserOutlined,
  RobotOutlined,
  CommentOutlined,
} from "@ant-design/icons";

const { Text, Title } = Typography;

// Добавим имя оператора для большей реалистичности
const OPERATOR_NAME = "Антон";

const cannedResponses = [
  {
    // ИЗМЕНЕНИЕ: Вставляем имя оператора в шаблон
    title: "Стандартное приветствие",
    text: `Добрый день! Меня зовут ${OPERATOR_NAME}, чем могу помочь?`,
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

// ПРИНИМАЕТ initialMessages как проп, который должен быть массивом с первым сообщением клиента
export default function SupportOperatorPage({ initialMessages = [] }) {
  const [message, setMessage] = useState("");
  const [isChatActive, setIsChatActive] = useState(true);

  // Инициализируем сообщения переданным массивом (с сообщением клиента)
  const [chatMessages, setChatMessages] = useState(initialMessages);

  // Состояния для модального окна ответа клиента
  const [isUserReplyModalVisible, setIsUserReplyModalVisible] = useState(false);
  const [userReplyText, setUserReplyText] = useState(
    "Спасибо, вы мне очень помогли!"
  );

  const messagesEndRef = useRef(null);

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  };

  // !!! ИСПРАВЛЕНИЕ !!!: Используем useEffect, чтобы гарантировать, что
  // если initialMessages изменится (включая первое обновление при монтировании),
  // состояние chatMessages будет установлено правильно.
  useEffect(() => {
    // Проверяем, что массив не пуст и он отличается от текущего состояния
    // Это исправляет проблему с отображением первого сообщения клиента
    if (initialMessages.length > 0) {
      setChatMessages(initialMessages);
    }
  }, [initialMessages]);

  // Эффект для прокрутки вниз
  useEffect(scrollToBottom, [chatMessages]);

  const handleTemplateClick = (text) => {
    if (isChatActive) {
      setMessage(text);
    }
  };

  const handleSend = () => {
    if (message.trim() === "" || !isChatActive) return;

    // 1. Отправляем сообщение оператора
    const operatorMessage = {
      id: Date.now(),
      text: message,
      sender: "operator",
    };
    setChatMessages((prev) => [...prev, operatorMessage]);
    setMessage("");
  };

  const handleSendUserReply = () => {
    if (userReplyText.trim() === "") return;

    const userReply = {
      id: Date.now() + 1,
      text: userReplyText,
      sender: "user",
    };

    setChatMessages((prev) => [...prev, userReply]);
    setUserReplyText("Спасибо, вы мне очень помогли!"); // Сброс на дефолт

    setIsUserReplyModalVisible(false); // Закрываем модальное окно
  };

  const handleKeyPress = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleEndChat = () => {
    setIsChatActive(false);
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
                onKeyPress={handleKeyPress}
                autoSize={{ minRows: 1, maxRows: 5 }}
                disabled={!isChatActive}
                style={{ flex: 1 }}
              />
              <Button
                type="primary"
                icon={<SendOutlined />}
                onClick={handleSend}
                disabled={!message.trim() || !isChatActive}
                style={{ marginRight: 8 }}
              >
                Ответить
              </Button>

              <Button
                icon={<CommentOutlined />}
                onClick={() => setIsUserReplyModalVisible(true)}
                disabled={!isChatActive}
                title="Имитировать ответ клиента"
                style={{ marginRight: 8 }}
              >
                Клиент
              </Button>

              <Button
                danger
                onClick={handleEndChat}
                disabled={!isChatActive}
                title="Завершить текущий диалог и заблокировать ввод"
              >
                Завершить
              </Button>
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

      {/* Модальное окно для ввода ответа клиента */}
      <Modal
        title="Имитация ответа клиента"
        open={isUserReplyModalVisible}
        onOk={handleSendUserReply}
        onCancel={() => setIsUserReplyModalVisible(false)}
        okText="Отправить как Клиент"
        cancelText="Отмена"
        centered // Добавлен этот пропс для вертикального центрирования
      >
        <Input.TextArea
          placeholder="Введите сообщение от лица клиента..."
          value={userReplyText}
          onChange={(e) => setUserReplyText(e.target.value)}
          autoSize={{ minRows: 3, maxRows: 5 }}
          style={{ marginTop: 16 }}
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
