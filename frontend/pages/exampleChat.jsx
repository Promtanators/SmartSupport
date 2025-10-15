import { Card, List, Avatar, Typography, Input, Button } from "antd";
import { SendOutlined } from "@ant-design/icons";
import { useState } from "react";

export default function ChatPage() {
  const [messages, setMessages] = useState([]);
  const [input, setInput] = useState("");

  const handleSend = () => {
    if (!input) return;
    setMessages([...messages, { text: input, user: "Вы" }]);
    setInput("");
  };

  return (
    <Card
      title="Чат"
      style={{
        width: 600,
        margin: "auto",
        height: "80vh",
        display: "flex",
        flexDirection: "column",
      }}
    >
      <List
        dataSource={messages}
        renderItem={(item) => (
          <List.Item>
            <List.Item.Meta
              avatar={<Avatar>{item.user[0]}</Avatar>}
              title={item.user}
              description={<Typography.Text>{item.text}</Typography.Text>}
            />
          </List.Item>
        )}
        style={{ flex: 1, overflowY: "auto" }}
      />
      <div style={{ display: "flex", gap: 8 }}>
        <Input.TextArea
          value={input}
          onChange={(e) => setInput(e.target.value)}
          autoSize={{ minRows: 1, maxRows: 4 }}
        />
        <Button type="primary" icon={<SendOutlined />} onClick={handleSend} />
      </div>
    </Card>
  );
}
