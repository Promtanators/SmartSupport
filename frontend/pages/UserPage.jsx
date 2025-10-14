import { Card, Button } from "antd";

export default function UserPage({ onNext }) {
  return (
    <Card title="User Page">
      <p>Базовое сообщение на UserPage</p>
      <Button type="primary" onClick={onNext}>
        Следующая страница
      </Button>
    </Card>
  );
}
