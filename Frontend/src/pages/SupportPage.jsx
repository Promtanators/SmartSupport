import { useState, useEffect, useRef } from "react";
import {
  Card,
  Input,
  Button,
  Avatar,
  Typography,
  Row,
  Col,
  Modal,
  Tag,
  Spin,
  Alert,
} from "antd";
import {
  SendOutlined,
  UserOutlined,
  CustomerServiceOutlined,
  CommentOutlined,
} from "@ant-design/icons";
import { fetchData } from "../api";

const RecommendationCard = ({ recommendation, onClick, isActive }) => {
  const getTagColor = (score) => {
    if (score > 80) return "green";
    if (score > 40) return "gold";
    return "red";
  };

  return (
    <Card
      size="small"
      hoverable={isActive}
      style={{
        marginBottom: 12,
        cursor: isActive ? "pointer" : "not-allowed",
        opacity: isActive ? 1 : 0.6,
      }}
      bodyStyle={{
        display: "flex",
        justifyContent: "space-between",
        alignItems: "center",
      }}
      onClick={isActive ? () => onClick(recommendation.answer) : undefined}
    >
      <Typography.Text
        style={{ flex: 1, paddingRight: "8px" }}
        type={isActive ? "" : "secondary"}
      >
        {recommendation.answer}
      </Typography.Text>
      <Tag color={getTagColor(recommendation.score)}>
        {recommendation.score}%
      </Tag>
    </Card>
  );
};

export default function SupportOperatorPage({
  message,
  data,
  error,
  setData,
  setError,
}) {
  const [supportInput, setSupportInput] = useState("");
  const [messages, setMessages] = useState(() =>
    message ? [{ id: Date.now(), text: message, sender: "user" }] : []
  );
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [userInput, setUserInput] = useState("");
  const [loadingRec, setLoadingRec] = useState(true);
  const messagesEndRef = useRef(null);

  const addMessage = (text, sender) => {
    if (!text.trim()) return;
    setMessages((prev) => [...prev, { id: Date.now(), text, sender }]);
  };

  useEffect(() => {
    messagesEndRef.current?.scrollIntoView({ behavior: "smooth" });
  }, [messages]);

  useEffect(() => {
    if (data || error) {
      const timer = setTimeout(() => setLoadingRec(false), 2500);
      return () => clearTimeout(timer);
    }
    if (!data && !error) {
      setLoadingRec(true);
    }
  }, [data, error]);

  const handleSend = () => {
    addMessage(supportInput, "operator");
    setSupportInput("");
  };

  const handleSendUserReply = async () => {
    if (!userInput.trim()) return;
    addMessage(userInput, "user");
    setIsModalOpen(false);
    setUserInput("");

    try {
      setLoadingRec(true);
      const result = await fetchData(userInput);
      setData(result);
      setError(null);
    } catch (err) {
      setError("Ошибка при получении подсказок");
      console.error(err);
    } finally {
      setLoadingRec(false);
    }
  };

  const handleKeyPress = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      handleSend();
    }
  };

  const handleModalKeyPress = (e) => {
    if (e.key === "Enter" && !e.shiftKey) {
      e.preventDefault();
      if (userInput.trim()) {
        handleSendUserReply();
      }
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
                        style={{ marginLeft: 8, flexShrink: 0 }}
                      />
                    )}
                  </div>
                </div>
              ))}
              <div ref={messagesEndRef} />
            </div>
            <div style={styles.inputArea}>
              <Input.TextArea
                placeholder={"Введите ответ клиенту..."}
                value={supportInput}
                onChange={(e) => setSupportInput(e.target.value)}
                onKeyDown={handleKeyPress}
                autoSize={{ minRows: 1, maxRows: 5 }}
                style={{ flex: 1 }}
              />
              <Button
                type="primary"
                icon={<SendOutlined />}
                onClick={handleSend}
                disabled={!supportInput.trim()}
                style={{ marginRight: 8 }}
              >
                Ответить
              </Button>
              <Button
                icon={<CommentOutlined />}
                onClick={() => setIsModalOpen(true)}
                style={{ marginRight: 8 }}
              >
                Клиент
              </Button>
            </div>
          </Card>
        </Col>

        <Col span={7} style={{ height: "100%" }}>
          <Card
            title="Рекомендации"
            style={styles.templateCard}
            styles={{ body: { overflowY: "auto", padding: "16px" } }}
          >
            {loadingRec && (
              <div style={{ textAlign: "center", padding: 32, height: "100%" }}>
                <Spin size="large" />
                <Typography.Text style={{ display: "block", marginTop: 16 }}>
                  Ищем ответ...
                </Typography.Text>
              </div>
            )}

            {!loadingRec && error && (
              <Alert
                message="Ошибка загрузки"
                description={error}
                type="error"
                showIcon
                style={{ margin: "16px 0" }}
              />
            )}

            {!loadingRec &&
              data?.recommendations
                ?.sort((a, b) => b.score - a.score)
                .map((rec, index) => (
                  <RecommendationCard
                    key={index}
                    recommendation={rec}
                    onClick={setSupportInput}
                    isActive={!loadingRec && !error}
                  />
                ))}

            {!loadingRec &&
              !error &&
              (!data ||
                !data.recommendations ||
                data.recommendations.length === 0) && (
                <Typography.Text type="secondary">
                  К сожалению, не сможем подсказать.
                </Typography.Text>
              )}
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
          onKeyDown={handleModalKeyPress}
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
    padding: "16px",
    overflow: "hidden",
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
