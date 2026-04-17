import styles from "./Button.module.css";

const Button = ({ text, onClick }) => {
    return (
        <div className={styles.button} onClick={onClick}>
            {text}
        </div>
    );
};

export default Button;
