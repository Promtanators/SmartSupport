import { Card, Input, Button } from "antd";
import { SendOutlined } from "@ant-design/icons";
import { useState } from "react";

import { Splitter } from "antd";

export default function SupportPage() {
  const [message, setMessage] = useState("");

  const handleSend = () => {
    console.log("Отправлено:", message);
    setMessage("");
  };

  return (
    <Splitter layout="horizontal" style={{ height: "96vh" }}>
      <Splitter.Panel
        defaultSize="80%"
        min="60%"
        max="85%"
        style={{
          display: "flex",
          flexDirection: "column",
          justifyContent: "flex-end",
        }}
      >
        
          <div style={{ display: "flex", gap: 8 }}>
            <Input.TextArea
              placeholder="Введите сообщение"
              style={{ flex: 1 }}
              autoSize={{ minRows: 1, maxRows: 4 }}
              value={message}
              onChange={(e) => setMessage(e.target.value)}
              
            />
            <Button
              type="primary"
              icon={<SendOutlined />}
              onClick={handleSend}
            />
          </div>
       
      </Splitter.Panel>

      <Splitter.Panel defaultSize="20%">Панель 2</Splitter.Panel>
    </Splitter>
  );
}
