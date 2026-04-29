import { useState } from "react";
import Button from "./Button";
import styles from "./Hamburger.module.css";

const Hamburger = ({ options }) => {
    const [isMenu, setIsMenu] = useState(false);

    return (
        <div className={styles.hamburgerContainer}>
            <div
                className={`${styles.hamburgerCircle} ${isMenu ? styles.active : ""}`}
                onClick={() => setIsMenu((prev) => !prev)}
            >
                <div className={styles.burgerIcon}>
                    <span></span>
                    <span></span>
                    <span></span>
                </div>
            </div>

            <div className={`${styles.hamburgerMenu} ${isMenu ? styles.show : ""}`}>
                {options &&
                    options.map((item, idx) => (
                        <Button key={"hamburger-item-" + idx} text={item[0]} onClick={item[1]} />
                    ))}
            </div>
        </div>
    );
};

export default Hamburger;
